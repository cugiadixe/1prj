using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PTKD.IntegrationTests;

namespace PTKD.ApiTests;

/// <summary>
/// API-test host that permits only PTKD_TEST_PHASE1A2 and verifies SELECT DB_NAME()
/// before a test client can issue a request that writes through the application.
/// </summary>
public class SafeTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string TestConnectionString =
        TestDatabaseSafety.ValidateConnectionString(TestDatabaseSafety.DefaultConnectionString);

    public string VerifiedDatabaseName { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString
            });
        });
        
        builder.ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(ProtectedTestController).Assembly);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        try
        {
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var validatedConnectionString = TestDatabaseSafety.ValidateConnectionString(connectionString);

            using var connection = TestDatabaseSafety.OpenVerifiedConnection(validatedConnectionString);
            VerifiedDatabaseName = TestDatabaseSafety.VerifyOpenConnection(connection);
            return host;
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }
}
