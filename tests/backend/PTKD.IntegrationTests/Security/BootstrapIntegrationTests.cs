using System;
using System.IO;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PTKD.Bootstrap;
using Xunit;

namespace PTKD.IntegrationTests.Security;

[Collection("Sequential")]
public class BootstrapIntegrationTests : IClassFixture<TestDatabaseFixture>, IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public BootstrapIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
        Environment.SetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD", null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
        Environment.SetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD", null);
    }

    [Fact]
    public void Bootstrap_Succeeds_AndSetsCorrectDatabaseState()
    {
        _fixture.ResetToV0004();

        var email = "admin@indevco.vn";
        var password = "StrongPassword123!";
        var args = new[]
        {
            "--BOOTSTRAP_ADMIN_NAME=Integration Admin",
            "--BOOTSTRAP_ADMIN_CODE=int_admin",
            $"--BOOTSTRAP_ADMIN_EMAIL={email}"
        };

        Environment.SetEnvironmentVariable("CONNECTION_STRING", _fixture.ConnectionString);
        Environment.SetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD", password);

        var result = PTKD.Bootstrap.Program.Main(args);

        // Assert success exit code
        Assert.Equal(0, result);

        using var connection = _fixture.OpenVerifiedConnection();

        // Assert User
        using (var cmd = new SqlCommand("SELECT id, employee_code, full_name, email FROM dbo.Users WHERE employee_code = 'int_admin'", connection))
        using (var reader = cmd.ExecuteReader())
        {
            Assert.True(reader.Read());
            Assert.Equal("Integration Admin", reader.GetString(2));
            Assert.Equal(email, reader.GetString(3));
        }

        // Assert User_Auth_Accounts
        long accountId = 0;
        using (var cmd = new SqlCommand("SELECT id, provider_type, must_change_password FROM dbo.User_Auth_Accounts WHERE provider_subject = @email", connection))
        {
            cmd.Parameters.AddWithValue("@email", email);
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            accountId = reader.GetInt64(0);
            Assert.Equal("INTERNAL", reader.GetString(1));
            Assert.True(reader.GetBoolean(2)); // MustChangePassword
        }

        // Assert Password_History
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Password_History WHERE account_id = @accountId", connection))
        {
            cmd.Parameters.AddWithValue("@accountId", accountId);
            Assert.Equal(1, (int)cmd.ExecuteScalar());
        }

        // Assert Admin_Groups
        long adminGroupId = 0;
        using (var cmd = new SqlCommand("SELECT id, name FROM dbo.Admin_Groups WHERE group_code = 'ADMIN_SECURITY'", connection))
        {
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            adminGroupId = reader.GetInt64(0);
            Assert.Equal("Security Administration", reader.GetString(1));
        }

        // Assert Admin_Group_Permissions
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Admin_Group_Permissions WHERE admin_group_id = @adminGroupId AND permission_code = 'SECURITY_ADMIN_MANAGE'", connection))
        {
            cmd.Parameters.AddWithValue("@adminGroupId", adminGroupId);
            Assert.Equal(1, (int)cmd.ExecuteScalar());
        }

        // Assert Security_Bootstrap_State
        using (var cmd = new SqlCommand("SELECT is_bootstrapped, bootstrapped_by_user_id FROM dbo.Security_Bootstrap_State WHERE id = 1", connection))
        {
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            Assert.True(reader.GetBoolean(0));
            Assert.NotNull(reader.GetValue(1));
        }

        // Assert Security_Audit_Events BOOTSTRAP_ADMIN_CREATED
        using (var cmd = new SqlCommand("SELECT after_state_json FROM dbo.Security_Audit_Events WHERE event_code = 'BOOTSTRAP_ADMIN_CREATED'", connection))
        {
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            var afterState = reader.GetString(0);
            Assert.Contains("int_admin", afterState);
        }

        // Second attempt should fail safely and return 2
        var result2 = PTKD.Bootstrap.Program.Main(args);
        Assert.Equal(2, result2);
    }

    [Fact]
    public void Bootstrap_FailsSafely_WhenDatabaseUnreachable()
    {
        var password = "StrongPassword123!";
        var args = new[]
        {
            "--BOOTSTRAP_ADMIN_NAME=Integration Admin",
            "--BOOTSTRAP_ADMIN_CODE=int_admin",
            "--BOOTSTRAP_ADMIN_EMAIL=admin@indevco.vn"
        };

        Environment.SetEnvironmentVariable("CONNECTION_STRING", "Server=localhost;Database=NonExistentDb;User Id=sa;Password=WrongPassword;TrustServerCertificate=True;");
        Environment.SetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD", password);

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            var result = PTKD.Bootstrap.Program.Main(args);
            
            // Assert exit code
            Assert.Equal(3, result);

            var output = sw.ToString();
            Assert.Contains("Bootstrap failed. See operational logs for details.", output);
            Assert.DoesNotContain("NonExistentDb", output); // Do not leak SQL Details
            Assert.DoesNotContain("WrongPassword", output); // Do not leak SQL Connection details
            Assert.DoesNotContain(password, output); // Do not leak admin password
            Assert.DoesNotContain("Exception", output); // No stack trace or Exception ToString
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
