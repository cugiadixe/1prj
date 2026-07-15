using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;
using System.IO;
using System.Linq;

namespace PTKD.IntegrationTests
{
    public class TestDatabaseFixture : IDisposable
    {
        public string ConnectionString { get; }

        public TestDatabaseFixture()
        {
            ConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                ?? "Server=localhost;Database=PTKD_TEST_PHASE1A2;Trusted_Connection=True;TrustServerCertificate=True;";
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            // Refuse if unexpected tables exist
            var cmdCheck = new SqlCommand(@"
                SELECT name FROM sys.tables 
                WHERE schema_id = SCHEMA_ID('dbo') 
                AND name NOT IN (
                    'SchemaVersions', 
                    'Users', 
                    'Companies', 
                    'Departments', 
                    'User_Company_Assignments', 
                    'User_Department_Assignments', 
                    'Employment_Histories'
                )", conn);
            
            using (var reader = cmdCheck.ExecuteReader())
            {
                var unexpectedTables = new List<string>();
                while (reader.Read())
                {
                    unexpectedTables.Add(reader.GetString(0));
                }
                if (unexpectedTables.Any())
                {
                    throw new InvalidOperationException($"Unexpected user tables found in PTKD_TEST_PHASE1A2: {string.Join(", ", unexpectedTables)}");
                }
            }

            // Clean up existing schema if it's there
            var u0002Path = Path.Combine("..", "..", "..", "..", "..", "..", "database", "rollbacks", "U0002__drop_organization_schema.sql");
            if (File.Exists(u0002Path))
            {
                // We bypass the 'V0002 must be recorded' check for the raw cleanup script by executing just the drop commands
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
            }

            var v0001Path = Path.Combine("..", "..", "..", "..", "..", "..", "database", "migrations", "V0001__create_schema_versions.sql");
            var v0002Path = Path.Combine("..", "..", "..", "..", "..", "..", "database", "migrations", "V0002__create_organization_schema.sql");
            
            ExecuteBatches(File.ReadAllText(v0001Path), conn);
            ExecuteBatches(File.ReadAllText(v0002Path), conn);

            // Record V0002 manually because we're running it raw here
            using var cmdInsert = new SqlCommand("INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0002', 'V0002__create_organization_schema.sql', 'APPLIED')", conn);
            cmdInsert.ExecuteNonQuery();
        }

        private void ExecuteBatches(string sql, SqlConnection conn)
        {
            var batches = sql.Split(new[] { "\r\nGO", "\nGO" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                using var cmd = new SqlCommand(batch, conn);
                cmd.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            // Cleanup done by specific test classes if needed
        }
    }

    [CollectionDefinition("Sequential")]
    public class SequentialCollection : ICollectionFixture<TestDatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
