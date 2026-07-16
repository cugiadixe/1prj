using Microsoft.Data.SqlClient;

namespace PTKD.IntegrationTests;

[Collection("Sequential")]
public sealed class DatabaseSafetyTests
{
    private readonly TestDatabaseFixture _fixture;

    public DatabaseSafetyTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void InitialCatalog_ExactGuard_AcceptsOnlyApprovedDatabase()
    {
        var validated = TestDatabaseSafety.ValidateConnectionString(_fixture.ConnectionString);
        var builder = new SqlConnectionStringBuilder(validated);

        Assert.Equal(
            TestDatabaseSafety.ApprovedDatabaseName,
            builder.InitialCatalog,
            ignoreCase: true);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PTKD_DEV")]
    [InlineData("PTKD_TEST_PHASE1A")]
    [InlineData("master")]
    [InlineData("model")]
    [InlineData("msdb")]
    [InlineData("tempdb")]
    [InlineData("PTKD_PROD")]
    [InlineData("PTKD_PRODUCTION")]
    [InlineData("PTKD_STAGING")]
    [InlineData("PTKD_UAT")]
    public void InitialCatalog_RejectsEveryNonApprovedDatabase(string databaseName)
    {
        var connectionString =
            $"Server=database-host-that-must-not-be-contacted;Database={databaseName};" +
            "Integrated Security=True;TrustServerCertificate=True;Connect Timeout=60;";

        var exception = Assert.Throws<InvalidOperationException>(
            () => TestDatabaseSafety.OpenVerifiedConnection(connectionString));

        Assert.Contains("InitialCatalog", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvironmentVariable_CannotBypassInitialCatalogGuard()
    {
        var original = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        try
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                "Server=database-host-that-must-not-be-contacted;Database=PTKD_DEV;Integrated Security=True;Connect Timeout=60;");

            Assert.Throws<InvalidOperationException>(TestDatabaseSafety.ResolveConnectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", original);
        }
    }

    [Fact]
    public void OpenConnection_VerifiesActualDbName()
    {
        using var connection = _fixture.OpenVerifiedConnection();

        Assert.Equal(
            TestDatabaseSafety.ApprovedDatabaseName,
            _fixture.LastVerifiedDatabaseName,
            ignoreCase: true);
        Assert.Equal(
            TestDatabaseSafety.ApprovedDatabaseName,
            TestDatabaseSafety.VerifyOpenConnection(connection),
            ignoreCase: true);
    }

    [Fact]
    public void DbNameVerification_RejectsUnexpectedReportedName()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => TestDatabaseSafety.RequireApprovedDatabaseName("PTKD_DEV", "DB_NAME()"));

        Assert.Contains("DB_NAME()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionCode_DoesNotAutomaticallyCreateDeleteOrMigrateDatabase()
    {
        var sourceFiles = Directory.GetFiles(
            Path.Combine(_fixture.RepositoryRoot, "src", "backend"),
            "*.cs",
            SearchOption.AllDirectories);
        var source = string.Join(Environment.NewLine, sourceFiles.Select(File.ReadAllText));

        Assert.DoesNotContain(".EnsureCreated(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".EnsureDeleted(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FixtureBaseline_ContainsV0001AndV0002ExactlyOnce()
    {
        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand(
            "SELECT Version, COUNT(*) FROM dbo.SchemaVersions GROUP BY Version;",
            connection);
        using var reader = command.ExecuteReader();
        var versions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            versions.Add(reader.GetString(0), reader.GetInt32(1));
        }

        Assert.Equal(1, versions["V0001"]);
        Assert.Equal(1, versions["V0002"]);
    }
}
