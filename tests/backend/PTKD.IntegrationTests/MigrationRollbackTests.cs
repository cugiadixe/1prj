using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;
using Xunit;

namespace PTKD.IntegrationTests
{
    [Collection("Sequential")]
    public class MigrationRollbackTests
    {
        private readonly string _connectionString;

        public MigrationRollbackTests()
        {
            _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                ?? "Server=localhost;Database=PTKD_TEST_PHASE1A;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        private string ExecuteDbMigrator(bool dryRun = false)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "dotnet";
            p.StartInfo.Arguments = $"run --project ../../../../../../src/backend/PTKD.DbMigrator/PTKD.DbMigrator.csproj" + (dryRun ? " -- --dry-run" : "");
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.EnvironmentVariables["ConnectionStrings__DefaultConnection"] = _connectionString;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                throw new Exception($"DbMigrator failed. Exit code: {p.ExitCode}. Output:\n{output}");
            }
            return output;
        }

        private void ExecuteU0002()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            var sql = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "..", "database", "rollbacks", "U0002__drop_organization_schema.sql"));
            var batches = sql.Split(new[] { "\r\nGO", "\nGO" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                using var cmd = new SqlCommand(batch, conn);
                cmd.ExecuteNonQuery();
            }
        }

        private int GetSchemaVersionsCount(string version)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand($"SELECT COUNT(*) FROM dbo.SchemaVersions WHERE Version = @V", conn);
            cmd.Parameters.AddWithValue("@V", version);
            return (int)cmd.ExecuteScalar();
        }

        private void CleanDatabaseAndApplyV0001()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // U0002 raw drop commands to ensure clean state
            var dropScript = @"
                    IF OBJECT_ID('dbo.FK_EmploymentHistories_created_by', 'F') IS NOT NULL ALTER TABLE dbo.Employment_Histories DROP CONSTRAINT FK_EmploymentHistories_created_by;
                    IF OBJECT_ID('dbo.FK_UserDepartmentAssignments_created_by', 'F') IS NOT NULL ALTER TABLE dbo.User_Department_Assignments DROP CONSTRAINT FK_UserDepartmentAssignments_created_by;
                    IF OBJECT_ID('dbo.FK_UserDepartmentAssignments_updated_by', 'F') IS NOT NULL ALTER TABLE dbo.User_Department_Assignments DROP CONSTRAINT FK_UserDepartmentAssignments_updated_by;
                    IF OBJECT_ID('dbo.FK_UserCompanyAssignments_created_by', 'F') IS NOT NULL ALTER TABLE dbo.User_Company_Assignments DROP CONSTRAINT FK_UserCompanyAssignments_created_by;
                    IF OBJECT_ID('dbo.FK_UserCompanyAssignments_updated_by', 'F') IS NOT NULL ALTER TABLE dbo.User_Company_Assignments DROP CONSTRAINT FK_UserCompanyAssignments_updated_by;
                    IF OBJECT_ID('dbo.FK_Departments_created_by', 'F') IS NOT NULL ALTER TABLE dbo.Departments DROP CONSTRAINT FK_Departments_created_by;
                    IF OBJECT_ID('dbo.FK_Departments_updated_by', 'F') IS NOT NULL ALTER TABLE dbo.Departments DROP CONSTRAINT FK_Departments_updated_by;
                    IF OBJECT_ID('dbo.FK_Companies_created_by', 'F') IS NOT NULL ALTER TABLE dbo.Companies DROP CONSTRAINT FK_Companies_created_by;
                    IF OBJECT_ID('dbo.FK_Companies_updated_by', 'F') IS NOT NULL ALTER TABLE dbo.Companies DROP CONSTRAINT FK_Companies_updated_by;
                    IF OBJECT_ID('dbo.FK_Users_created_by', 'F') IS NOT NULL ALTER TABLE dbo.Users DROP CONSTRAINT FK_Users_created_by;
                    IF OBJECT_ID('dbo.FK_Users_updated_by', 'F') IS NOT NULL ALTER TABLE dbo.Users DROP CONSTRAINT FK_Users_updated_by;

                    DROP TABLE IF EXISTS dbo.Employment_Histories;
                    DROP TABLE IF EXISTS dbo.User_Department_Assignments;
                    DROP TABLE IF EXISTS dbo.User_Company_Assignments;
                    DROP TABLE IF EXISTS dbo.Departments;
                    DROP TABLE IF EXISTS dbo.Companies;
                    DROP TABLE IF EXISTS dbo.Users;
                    DROP TABLE IF EXISTS dbo.SchemaVersions;
            ";
            using var cmdDrop = new SqlCommand(dropScript, conn);
            cmdDrop.ExecuteNonQuery();

            // Apply V0001
            var sql = File.ReadAllText(Path.Combine("..", "..", "..", "..", "..", "..", "database", "migrations", "V0001__create_schema_versions.sql"));
            var batches = sql.Split(new[] { "\r\nGO", "\nGO" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                using var cmd = new SqlCommand(batch, conn);
                cmd.ExecuteNonQuery();
            }

            using var cmdInsert = new SqlCommand("INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0001', 'V0001__create_schema_versions.sql', 'APPLIED')", conn);
            cmdInsert.ExecuteNonQuery();
        }

        [Fact]
        public void DbMigratorAtomicityAndIdempotencyAndRollbackFlow()
        {
            CleanDatabaseAndApplyV0001();

            // 1. Verify exactly one V0002 SchemaVersions record after apply
            var output = ExecuteDbMigrator();
            if (!output.Contains("Applied V0002")) 
                throw new Exception($"DbMigrator didn't apply V0002. Output: {output}");
            Assert.Equal(1, GetSchemaVersionsCount("V0002"));

            // Verify tables exist
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = 'Users'", conn);
                Assert.Equal(1, (int)cmd.ExecuteScalar());
            }

            // 2. Verifies V0002 is skipped on a second apply (idempotency)
            ExecuteDbMigrator();
            Assert.Equal(1, GetSchemaVersionsCount("V0002")); // Should still be exactly 1

            // 3. U0002 rejection when a later numeric migration exists
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0003', 'V0003__fake.sql', 'APPLIED')", conn);
                cmd.ExecuteNonQuery();
            }

            var ex = Assert.Throws<SqlException>(() => ExecuteU0002());
            Assert.Contains("A migration later than V0002 exists", ex.Message);

            // Cleanup the fake V0003
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("DELETE FROM dbo.SchemaVersions WHERE Version = 'V0003'", conn);
                cmd.ExecuteNonQuery();
            }

            // 4. Applies U0002 manually and verifies it removes only V0002 from SchemaVersions, preserving V0001
            ExecuteU0002();
            Assert.Equal(0, GetSchemaVersionsCount("V0002"));
            Assert.Equal(1, GetSchemaVersionsCount("V0001"));

            // Verify tables are gone
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = 'Users'", conn);
                Assert.Equal(0, (int)cmd.ExecuteScalar());
            }

            // 5. U0002 rejection when V0002 is not recorded
            var ex2 = Assert.Throws<SqlException>(() => ExecuteU0002());
            Assert.Contains("V0002 is not recorded in SchemaVersions", ex2.Message);

            // 6. Verifies V0002 successfully reapplies after U0002
            ExecuteDbMigrator();
            Assert.Equal(1, GetSchemaVersionsCount("V0002"));
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = 'Users'", conn);
                Assert.Equal(1, (int)cmd.ExecuteScalar());
            }
        }
        
        [Fact]
        public void DbMigratorRollsBackWhenScriptFails()
        {
            CleanDatabaseAndApplyV0001();
            
            // Create a fake broken migration
            var badMigrationPath = Path.Combine("..", "..", "..", "..", "..", "..", "database", "migrations", "V9999__bad_migration.sql");
            File.WriteAllText(badMigrationPath, "CREATE TABLE dbo.TestBad (id int);\nGO\nSELECT * FROM NonExistentTable;\nGO");

            try
            {
                // This will fail on the second batch
                Assert.Throws<Exception>(() => ExecuteDbMigrator());

                // The transaction should have rolled back the first batch
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = 'TestBad'", conn);
                Assert.Equal(0, (int)cmd.ExecuteScalar());

                // No SchemaVersions record when migration execution fails
                var cmd2 = new SqlCommand("SELECT COUNT(*) FROM dbo.SchemaVersions WHERE Version = 'V9999'", conn);
                Assert.Equal(0, (int)cmd2.ExecuteScalar());
            }
            finally
            {
                if (File.Exists(badMigrationPath))
                {
                    File.Delete(badMigrationPath);
                }
            }
        }
    }
}
