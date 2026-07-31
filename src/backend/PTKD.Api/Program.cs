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
using PTKD.Infrastructure.Security.Audit;
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
    options.Filters.Add<PTKD.Api.Security.Authorization.MustChangePasswordAuthorizationFilter>();
    options.Filters.Add<PTKD.Api.Security.Authorization.PermissionAuthorizationFilter>();
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
builder.Services.AddScoped<PTKD.Application.Security.Authorization.Interfaces.IAuthorizationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

// Authorization Services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PTKD.Application.Security.Authorization.Interfaces.IPermissionEvaluator, PTKD.Application.Security.Authorization.Services.PermissionEvaluator>();
builder.Services.AddScoped<PTKD.Application.Security.Authorization.Interfaces.ISecurityAdminService, PTKD.Application.Security.Authorization.Services.SecurityAdminService>();

// Application Services
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserAssignmentService, UserAssignmentService>();

// Customer Services (Phase 1B.2-B1)
builder.Services.AddScoped<PTKD.Application.Customers.Services.ICustomerService, PTKD.Application.Customers.Services.CustomerService>();

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

// Account Management Service (Phase 1B.1-I)
builder.Services.AddScoped<PTKD.Application.Security.AccountManagement.IAccountManagementService, PTKD.Infrastructure.Security.AccountManagement.AccountManagementService>();

// Audit Services (Phase 1B.1-F-A & 1B.1-H)
builder.Services.AddScoped<PTKD.Application.Security.Audit.IAuditWriter, SqlSecurityAuditWriter>();
builder.Services.AddScoped<PTKD.Application.Security.Audit.ITransactionalAuditWriter, SqlTransactionalAuditWriter>();
builder.Services.AddScoped<PTKD.Application.Security.Audit.ISecurityAuditQueryService, SqlSecurityAuditQueryService>();

// CSRF (Phase 1B.1-C-B)
builder.Services.AddScoped<CsrfTokenService>();

// Authentication & JWT Bearer
builder.Services.AddScoped<IProtectedRequestValidator, ProtectedRequestValidator>();

// Authentication & JWT Bearer
builder.Services.ConfigureOptions<JwtBearerConfigureOptions>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

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
