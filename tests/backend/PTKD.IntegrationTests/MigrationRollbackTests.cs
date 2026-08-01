using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace PTKD.IntegrationTests;

[Collection("Sequential")]
public sealed class MigrationRollbackTests
{
    private readonly TestDatabaseFixture _fixture;

    public MigrationRollbackTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void DbMigrator_AppliesExactlyOnce_ThenRollsBackInDependencyOrder()
    {
        _fixture.ResetToEmpty();

        var firstOutput = ExecuteDbMigrator();
        Assert.Contains("Applied V0001", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0002", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0004", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0005", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0006", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0007", firstOutput, StringComparison.Ordinal);
        Assert.Equal(1, GetSchemaVersionsCount("V0001"));
        Assert.Equal(1, GetSchemaVersionsCount("V0002"));
        Assert.Equal(1, GetSchemaVersionsCount("V0003"));
        Assert.Equal(1, GetSchemaVersionsCount("V0004"));

        Assert.Equal(1, GetSchemaVersionsCount("V0005"));
        Assert.Equal(1, GetSchemaVersionsCount("V0006"));
        Assert.Equal(1, GetSchemaVersionsCount("V0007"));

        var secondOutput = ExecuteDbMigrator();
        Assert.Contains("Skipping V0001", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0002", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0003", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0004", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0005", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0006", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0007", secondOutput, StringComparison.Ordinal);
        Assert.Equal(1, GetSchemaVersionsCount("V0007"));

        ExecuteRollback("U0007__drop_customer_change_request.sql");
        Assert.Equal(0, GetSchemaVersionsCount("V0007"));
        Assert.Equal(1, GetSchemaVersionsCount("V0006"));

        ExecuteRollback("U0006__drop_workflow_schema.sql");
        Assert.Equal(0, GetSchemaVersionsCount("V0006"));
        Assert.Equal(1, GetSchemaVersionsCount("V0005"));

        ExecuteRollback("U0005__drop_customer_schema.sql");
        Assert.Equal(0, GetSchemaVersionsCount("V0005"));
        Assert.Equal(1, GetSchemaVersionsCount("V0004"));

        ExecuteRollback("U0004__deactivate_security_admin_manage_permission.sql");
        Assert.Equal(0, GetSchemaVersionsCount("V0004"));
        Assert.Equal(1, GetSchemaVersionsCount("V0003"));

        var ex = Assert.Throws<Microsoft.Data.SqlClient.SqlException>(() =>
            ExecuteRollback("U0003__drop_security_schema.sql"));
        Assert.Contains("Permissions differs from the approved seed catalog", ex.Message);
    }

    [Fact]
    public void DbMigrator_RollsBackEntireFailedMigration()
    {
        _fixture.ResetToEmpty();
        ExecuteDbMigrator();

        var badMigrationPath = Path.Combine(
            _fixture.RepositoryRoot,
            "database",
            "migrations",
            "V9999__test_atomicity_failure.sql");
        Assert.False(File.Exists(badMigrationPath), $"Unexpected existing test migration: {badMigrationPath}");

        File.WriteAllText(
            badMigrationPath,
            "CREATE TABLE dbo.TestBadMigrationAtomicity (id int NOT NULL);\nGO\n" +
            "SELECT * FROM dbo.ThisTableMustNotExist;\nGO\n");

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(ExecuteDbMigrator);
            Assert.Contains("transaction rolled back", exception.Message, StringComparison.OrdinalIgnoreCase);

            using var connection = _fixture.OpenVerifiedConnection();
            using var command = new SqlCommand(
                "SELECT COUNT(*) FROM sys.tables WHERE name = N'TestBadMigrationAtomicity';",
                connection);
            Assert.Equal(0, Convert.ToInt32(command.ExecuteScalar()));
            Assert.Equal(0, GetSchemaVersionsCount("V9999"));
        }
        finally
        {
            File.Delete(badMigrationPath);
        }
    }

    private string ExecuteDbMigrator()
    {
        var validatedConnectionString = TestDatabaseSafety.ValidateConnectionString(_fixture.ConnectionString);
        var projectPath = Path.Combine(
            _fixture.RepositoryRoot,
            "src",
            "backend",
            "PTKD.DbMigrator",
            "PTKD.DbMigrator.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _fixture.RepositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Debug");
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.Environment["ConnectionStrings__DefaultConnection"] = validatedConnectionString;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start PTKD.DbMigrator.");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var output = standardOutput + Environment.NewLine + standardError;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"DbMigrator failed with exit code {process.ExitCode}. Output:{Environment.NewLine}{output}");
        }

        return output;
    }

    private void ExecuteRollback(string fileName)
    {
        using var connection = _fixture.OpenVerifiedConnection();
        TestDatabaseFixture.ExecuteBatches(_fixture.ReadRollback(fileName), connection);
    }

    private int GetSchemaVersionsCount(string version)
    {
        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.SchemaVersions WHERE Version = @version;",
            connection);
        command.Parameters.AddWithValue("@version", version);
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
