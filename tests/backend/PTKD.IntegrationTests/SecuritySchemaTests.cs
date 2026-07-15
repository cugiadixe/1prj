using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;
using System.IO;

namespace PTKD.IntegrationTests
{
    [Collection("Sequential")]
    public class SecuritySchemaTests
    {
        private readonly TestDatabaseFixture _fixture;

        public SecuritySchemaTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void DatabaseSafety_Is_PTKD_TEST_PHASE1A2()
        {
            var builder = new SqlConnectionStringBuilder(_fixture.ConnectionString);
            Assert.Equal("PTKD_TEST_PHASE1A2", builder.InitialCatalog, ignoreCase: true);
        }

        [Fact]
        public void V0003_Executes_ExactlyOnce_And_U0003_RollsBack_Safely()
        {
            var v0003Path = Path.Combine("..", "..", "..", "..", "..", "..", "database", "migrations", "V0003__create_security_schema.sql");
            var u0003Path = Path.Combine("..", "..", "..", "..", "..", "..", "database", "rollbacks", "U0003__drop_security_schema.sql");
            
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();

            // Run V0003
            ExecuteBatches(File.ReadAllText(v0003Path), conn);
            
            // Mark it applied
            using (var cmdMark = new SqlCommand("INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0003', 'V0003__create_security_schema.sql', 'APPLIED')", conn))
            {
                cmdMark.ExecuteNonQuery();
            }

            // Verify a table exists
            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = 'User_Auth_Accounts'", conn);
            Assert.Equal(1, (int)checkCmd.ExecuteScalar());

            // Run U0003
            ExecuteBatches(File.ReadAllText(u0003Path), conn);

            // Verify table is gone
            checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = 'User_Auth_Accounts'", conn);
            Assert.Equal(0, (int)checkCmd.ExecuteScalar());

            // Verify SchemaVersion is gone
            var versionCheck = new SqlCommand("SELECT COUNT(*) FROM dbo.SchemaVersions WHERE Version = 'V0003'", conn);
            Assert.Equal(0, (int)versionCheck.ExecuteScalar());

            // Re-run V0003 so other tests can pass against the expected schema state
            ExecuteBatches(File.ReadAllText(v0003Path), conn);
            using (var cmdMark = new SqlCommand("INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0003', 'V0003__create_security_schema.sql', 'APPLIED')", conn))
            {
                cmdMark.ExecuteNonQuery();
            }
        }

        [Fact]
        public void AuditEvents_CannotBe_DeletedOrUpdated()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();

            // Insert one
            using (var cmd = new SqlCommand("INSERT INTO dbo.Security_Audit_Events (event_type) VALUES ('TEST_EVENT'); SELECT SCOPE_IDENTITY();", conn))
            {
                var id = (decimal)cmd.ExecuteScalar();
                Assert.True(id > 0);

                // Try update
                using (var updateCmd = new SqlCommand($"UPDATE dbo.Security_Audit_Events SET event_type = 'MODIFIED' WHERE id = {id}", conn))
                {
                    var ex = Assert.Throws<SqlException>(() => updateCmd.ExecuteNonQuery());
                    Assert.Contains("UPDATE and DELETE are prohibited", ex.Message);
                }

                // Try delete
                using (var deleteCmd = new SqlCommand($"DELETE FROM dbo.Security_Audit_Events WHERE id = {id}", conn))
                {
                    var ex = Assert.Throws<SqlException>(() => deleteCmd.ExecuteNonQuery());
                    Assert.Contains("UPDATE and DELETE are prohibited", ex.Message);
                }
            }
        }

        [Fact]
        public void Role_Scope_Constraints_Enforced()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();

            // GLOBAL role with non-null company should fail
            using (var cmd = new SqlCommand("INSERT INTO dbo.Roles (role_code, scope_type, company_id) VALUES ('BAD_GLOBAL', 'GLOBAL', 999)", conn))
            {
                var ex = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());
                Assert.Contains("CHK_Roles_ScopeCompany", ex.Message);
            }

            // COMPANY role with null company should fail
            using (var cmd = new SqlCommand("INSERT INTO dbo.Roles (role_code, scope_type, company_id) VALUES ('BAD_COMPANY', 'COMPANY', NULL)", conn))
            {
                var ex = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());
                Assert.Contains("CHK_Roles_ScopeCompany", ex.Message);
            }
        }

        [Fact]
        public void UserRoleAssignments_Cannot_Have_Overlapping_Active_Records()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();

            // Insert dummy user/role
            var userId = InsertDummyUser(conn);
            var roleId = InsertDummyRole(conn);

            using (var cmd = new SqlCommand($"INSERT INTO dbo.User_Role_Assignments (user_id, role_id, assignment_status) VALUES ({userId}, {roleId}, 'ACTIVE')", conn))
            {
                cmd.ExecuteNonQuery();
            }

            // Inserting a second active one for the same user and role should fail uniqueness
            using (var cmd = new SqlCommand($"INSERT INTO dbo.User_Role_Assignments (user_id, role_id, assignment_status) VALUES ({userId}, {roleId}, 'ACTIVE')", conn))
            {
                var ex = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());
                Assert.Contains("UQ_UserRole_ActiveOverlap", ex.Message);
            }
        }

        private long InsertDummyUser(SqlConnection conn)
        {
            using var cmd = new SqlCommand("INSERT INTO dbo.Users (login_name, email, is_active, row_version, created_at, account_status, employment_status) VALUES (NEWID(), NEWID(), 1, 1, SYSUTCDATETIME(), 'ACTIVE', 'ACTIVE'); SELECT SCOPE_IDENTITY();", conn);
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        private long InsertDummyRole(SqlConnection conn)
        {
            using var cmd = new SqlCommand("INSERT INTO dbo.Roles (role_code, scope_type) VALUES (NEWID(), 'GLOBAL'); SELECT SCOPE_IDENTITY();", conn);
            return Convert.ToInt64(cmd.ExecuteScalar());
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
    }
}
