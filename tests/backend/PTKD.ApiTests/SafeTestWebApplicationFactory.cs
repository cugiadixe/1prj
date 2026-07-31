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
/// <remarks>
/// Schema initialization (ResetToV0005) is performed exactly once per test process via
/// a static lazy guard.  Derived factories produced by WithWebHostBuilder() share the
/// same static state and therefore do NOT re-run the destructive reset on every
/// CreateHost() invocation, which previously caused table-dropped deadlocks mid-run.
/// </remarks>
public class SafeTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string TestConnectionString =
        TestDatabaseSafety.ValidateConnectionString(TestDatabaseSafety.DefaultConnectionString);

    /// <summary>
    /// Ensures the database schema is reset to V0003 exactly once per test process.
    /// Lazy&lt;T&gt; guarantees thread-safe single execution even if multiple factories
    /// are constructed concurrently.
    /// </summary>
    private static readonly Lazy<bool> SchemaInitialized = new(() =>
    {
        new TestDatabaseFixture().ResetToV0005();
        return true;
    });

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

            // Ensure V0003 schema is present exactly once per test process.
            // The Lazy guard prevents re-running ResetToV0005 on every CreateHost()
            // call (which happens each time WithWebHostBuilder creates a derived factory).
            _ = SchemaInitialized.Value;

            return host;
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }
}
