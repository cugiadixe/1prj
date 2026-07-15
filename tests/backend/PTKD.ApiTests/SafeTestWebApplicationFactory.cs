using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PTKD.ApiTests;

/// <summary>
/// Safe WebApplicationFactory that ALWAYS configures:
/// - Environment = "Testing"
/// - ConnectionStrings:DefaultConnection → PTKD_TEST_PHASE1A2
/// - Rejects any connection string containing "PTKD_DEV"
///
/// No API test may bypass this factory.
/// </summary>
public class SafeTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestDatabase = "PTKD_TEST_PHASE1A2";
    private const string ForbiddenDatabase = "PTKD_DEV";

    private static readonly string TestConnectionString =
        $"Server=localhost;Database={TestDatabase};Trusted_Connection=True;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment BEFORE any service resolution
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Remove all existing configuration sources that might contain
            // user secrets pointing to PTKD_DEV
            config.Sources.Clear();

            // Add only the test connection string
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(
                    "ConnectionStrings:DefaultConnection",
                    TestConnectionString)
            });
        });

        // After all configuration is applied, verify the resolved connection string
        builder.ConfigureServices(services =>
        {
            // Build a temporary configuration to verify the database name
            var sp = services.BuildServiceProvider();
            var config = sp.GetService(typeof(IConfiguration)) as IConfiguration;
            var connStr = config?.GetConnectionString("DefaultConnection") ?? "";

            if (connStr.Contains(ForbiddenDatabase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"API test connection string resolves to forbidden database '{ForbiddenDatabase}'. " +
                    $"Tests must target '{TestDatabase}' only.");
            }

            if (!connStr.Contains(TestDatabase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"API test connection string does not contain '{TestDatabase}'. " +
                    $"Resolved: {connStr}");
            }
        });
    }
}
