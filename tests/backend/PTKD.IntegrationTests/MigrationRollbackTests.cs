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
        // V0001/V0002 dựng sẵn + seed công ty '-HN' để migrator áp tiếp từ V0003 qua được V0038.
        _fixture.ResetToV0002WithHanoiCompany();

        var firstOutput = ExecuteDbMigrator();
        Assert.Contains("Skipping V0001", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0002", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0004", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0005", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0006", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0007", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0008", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0009", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0010", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0011", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0012", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0013", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0014", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0015", firstOutput, StringComparison.Ordinal);
        Assert.Equal(1, GetSchemaVersionsCount("V0001"));
        Assert.Equal(1, GetSchemaVersionsCount("V0002"));
        Assert.Equal(1, GetSchemaVersionsCount("V0003"));
        Assert.Equal(1, GetSchemaVersionsCount("V0004"));

        Assert.Equal(1, GetSchemaVersionsCount("V0005"));
        Assert.Equal(1, GetSchemaVersionsCount("V0006"));
        Assert.Equal(1, GetSchemaVersionsCount("V0007"));
        Assert.Equal(1, GetSchemaVersionsCount("V0008"));
        Assert.Equal(1, GetSchemaVersionsCount("V0009"));
        Assert.Equal(1, GetSchemaVersionsCount("V0010"));
        Assert.Equal(1, GetSchemaVersionsCount("V0011"));
        Assert.Equal(1, GetSchemaVersionsCount("V0012"));
        Assert.Equal(1, GetSchemaVersionsCount("V0013"));
        Assert.Equal(1, GetSchemaVersionsCount("V0014"));
        Assert.Equal(1, GetSchemaVersionsCount("V0015"));

        var secondOutput = ExecuteDbMigrator();
        Assert.Contains("Skipping V0001", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0002", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0003", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0004", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0005", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0006", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0007", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0008", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0009", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0010", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0011", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0012", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0013", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0014", secondOutput, StringComparison.Ordinal);
        Assert.Contains("Skipping V0015", secondOutput, StringComparison.Ordinal);
        Assert.Equal(1, GetSchemaVersionsCount("V0015"));

        // Rollback lùi-giữa-chuỗi (U0015->U0004) KHÔNG còn khả thi khi migrator áp TOÀN BỘ chuỗi:
        // rollback script chỉ có tới U0015, các migration V0016-V0045 là forward-only (không có
        // U-file), nên không thể lùi bảng ở giữa mà không vỡ phụ thuộc của migration trên nó.
        // Cơ chế rollback đã được phủ bởi: DbMigrator_RollsBackEntireFailedMigration (rollback
        // nguyên tử khi 1 migration lỗi) và SecuritySchemaTests.Rollback_CleanSchemaSucceeds
        // (rollback U0003 rồi migrate lại). Test này chỉ còn khẳng định: áp đúng-một-lần + idempotent.
    }

    [Fact]
    public void DbMigrator_RollsBackEntireFailedMigration()
    {
        _fixture.ResetToV0002WithHanoiCompany();
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
