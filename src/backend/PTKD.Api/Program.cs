using System;
using System.Linq;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;
using PTKD.API.Filters;
using PTKD.Api.Security;

using PTKD.Application.Common.Interfaces;
using PTKD.Application.Organizations.Assignments.Services;
using PTKD.Application.Organizations.Companies.Services;
using PTKD.Application.Organizations.Departments.Services;
using PTKD.Application.Organizations.Users.Services;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Services;
using PTKD.Domain.Security.Authentication;
using PTKD.Infrastructure.Persistence;
using PTKD.Infrastructure.Persistence.Interceptors;
using PTKD.Infrastructure.Persistence.Retries;
using PTKD.Infrastructure.Security.Authentication;
using PTKD.Infrastructure.Security.Cryptography;
using PTKD.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(), preserveStaticLogger: true);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
    options.Filters.Add<ValidationFilter>();
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ProblemDetails
builder.Services.AddProblemDetails();

// CORS - allow frontend dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// DbContext and Infrastructure
builder.Services.AddSingleton<AppendOnlyInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection") ?? "";
    options.UseSqlServer(connStr, sqlOptions =>
    {
        sqlOptions.ExecutionStrategy(c => new DeadlockRetryPolicy(c, 2, TimeSpan.FromMilliseconds(500)));
    });
    options.AddInterceptors(sp.GetRequiredService<AppendOnlyInterceptor>());
});
builder.Services.AddScoped<IOrganizationDbContextFactory, AppDbContextFactory>();
builder.Services.AddScoped<IAuthenticationDbContextFactory, AuthenticationDbContextFactory>();
builder.Services.AddScoped<ITokenSessionDbContextFactory, TokenSessionDbContextFactory>();

// Application Services
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserAssignmentService, UserAssignmentService>();

// Authentication Services (Phase 1B.1-C-B)
builder.Services.AddSingleton<AuthenticationAccountPolicy>();
builder.Services.AddScoped<IPasswordHashService, AspNetCorePasswordHashService>();
builder.Services.AddScoped<IProviderSubjectNormalizer, InternalProviderSubjectNormalizer>();
builder.Services.AddScoped<ISessionInvalidationService, SecurityStampSessionInvalidationService>();
builder.Services.AddScoped<IUtcClock, SystemUtcClock>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<IAuthenticationAccountService, AuthenticationAccountService>();
builder.Services.AddSingleton<IJwtSigningKeyProvider, JwtSigningKeyProvider>();
builder.Services.AddScoped<IJwtAccessTokenService, JwtAccessTokenService>();
builder.Services.AddScoped<IRefreshTokenMaterialService, RefreshTokenMaterialService>();
builder.Services.AddScoped<ITokenSessionLifecycleService, TokenSessionLifecycleService>();

// CSRF (Phase 1B.1-C-B)
builder.Services.AddScoped<CsrfTokenService>();

// Authentication & JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Require HTTPS for metadata
        options.RequireHttpsMetadata = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "PTKD-ERP", // Default issuer

            ValidateAudience = true,
            ValidAudience = "PTKD-ERP-API", // Default audience

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidateIssuerSigningKey = true,
            // The signing key will be resolved dynamically using the kid from the header
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                var provider = builder.Services.BuildServiceProvider().GetRequiredService<IJwtSigningKeyProvider>();
                var keyDesc = provider.GetValidationKeys().FirstOrDefault(k => k.Kid == kid);
                if (keyDesc != null)
                {
                    var rsa = System.Security.Cryptography.RSA.Create();
                    rsa.ImportRSAPublicKey(keyDesc.PublicKeyBytes, out _);
                    return new[] { new RsaSecurityKey(rsa) { KeyId = kid } };
                }
                return Enumerable.Empty<SecurityKey>();
            }
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // TODO (Phase 1B.1-C-C):
                // Fully wire protected-request stamp validation here or via a dedicated authorization policy/filter.
                // This requires extracting the `sub` (UserId) and `security_stamp` claims from the JWT,
                // querying the database to verify the account is ACTIVE, employment status is eligible,
                // the `security_stamp` matches, and the token was issued after `sessions_invalidated_at`.
                // For Phase 1B.1-C-B, we only implement the cryptographic and standard claim validations.
                return Task.CompletedTask;
            }
        };
    });

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<PTKD.Application.Organizations.Companies.Validations.CreateCompanyRequestValidator>();

// Health checks
var healthChecks = builder.Services.AddHealthChecks();
healthChecks.AddSqlServer(
    sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection") ?? "",
    name: "sql_server", tags: ["db"]);

var app = builder.Build();

app.UseSerilogRequestLogging();

// Correlation ID Middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();
    if (string.IsNullOrEmpty(correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
        context.Request.Headers.Append("X-Correlation-ID", correlationId);
    }
    context.Response.Headers.Append("X-Correlation-ID", correlationId);

    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

// Global exception handling
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Environment Protection for Organization APIs
// Handled by EnvironmentProtectionConvention

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    // Do not map organization controllers in Production/Staging.
    // Since this is Phase 1A.2, we just throw an exception to fail startup per requirements.
    // Or, we can use a custom constraint/convention to remove them. But the easiest way to prevent them from starting is failing.
    throw new InvalidOperationException("Unsafe organization API configuration enabled.");
}

app.MapControllers();

// Endpoint GET /api/v2/health
app.MapHealthChecks("/api/v2/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString()
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();

// For integration tests
public partial class Program { }
