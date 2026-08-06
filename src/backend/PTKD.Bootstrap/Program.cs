using System;
using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PTKD.Domain.Entities;
using PTKD.Infrastructure.Security.Authentication;

namespace PTKD.Bootstrap;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("PTKD Initial Admin Bootstrap");
        Console.WriteLine("============================");
        
        var envConfig = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var cliConfig = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();

        if (cliConfig["BOOTSTRAP_ADMIN_PASSWORD"] != null)
        {
            Console.WriteLine("Error: BOOTSTRAP_ADMIN_PASSWORD must not be provided via command-line arguments for security reasons. Use environment variables instead.");
            return 1;
        }

        if (cliConfig["CONNECTION_STRING"] != null)
        {
            Console.WriteLine("Error: CONNECTION_STRING must not be provided via command-line arguments for security reasons. Use environment variables instead.");
            return 1;
        }

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var connectionString = envConfig["CONNECTION_STRING"];
        var adminName = config["BOOTSTRAP_ADMIN_NAME"] ?? "System Administrator";
        var adminCode = config["BOOTSTRAP_ADMIN_CODE"] ?? "admin";
        var adminEmail = config["BOOTSTRAP_ADMIN_EMAIL"];
        var adminPassword = envConfig["BOOTSTRAP_ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("Error: CONNECTION_STRING is required.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            Console.WriteLine("Error: BOOTSTRAP_ADMIN_EMAIL is required.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            Console.WriteLine("Error: BOOTSTRAP_ADMIN_PASSWORD is required.");
            return 1;
        }

        try
        {
            // Sanitize inputs for logging
            Console.WriteLine($"Configured admin email: {adminEmail}");
            Console.WriteLine($"Configured admin code: {adminCode}");

            RunBootstrap(connectionString, adminName, adminCode, adminEmail, adminPassword);
            Console.WriteLine("Bootstrap completed successfully.");
            return 0;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Already bootstrapped"))
        {
            Console.WriteLine("System is already bootstrapped. No action taken.");
            return 2;
        }
        catch (Exception)
        {
            Console.WriteLine("Bootstrap failed. See operational logs for details.");
            // DO NOT log full stack trace or ex.Message to avoid secret leakage in generic dumps.
            return 3;
        }
    }

    private static void RunBootstrap(
        string connectionString, 
        string name, 
        string code, 
        string email, 
        string password)
    {
        var utcNow = DateTime.UtcNow;

        var hasher = new AspNetCorePasswordHashService();
        var dummyAccount = UserAuthAccount.CreateInternal(1, email, "dummy", utcNow);
        var passwordHash = hasher.HashPassword(dummyAccount, password);

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // 1. SERIALIZABLE transaction to lock Security_Bootstrap_State and ensure safe concurrent execution
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        // Check if already bootstrapped
        using (var cmd = new SqlCommand("SELECT is_bootstrapped FROM dbo.Security_Bootstrap_State WHERE id = 1", connection, transaction))
        {
            var isBootstrapped = (bool?)cmd.ExecuteScalar();
            if (isBootstrapped == null)
            {
                throw new InvalidOperationException("Security_Bootstrap_State row id=1 is missing. Database schema is invalid.");
            }
            if (isBootstrapped.Value)
            {
                throw new InvalidOperationException("Already bootstrapped.");
            }
        }

        // Validate SECURITY_ADMIN_MANAGE exists
        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Permissions WHERE permission_code = 'SECURITY_ADMIN_MANAGE'", connection, transaction))
        {
            var count = (int)cmd.ExecuteScalar();
            if (count == 0)
            {
                throw new InvalidOperationException("SECURITY_ADMIN_MANAGE permission is missing. Cannot proceed with bootstrap.");
            }
        }

        long userId;
        long accountId;
        long adminGroupId;

        // Insert User
        using (var cmd = new SqlCommand(@"
            INSERT INTO dbo.Users (employee_code, full_name, email, employment_status, account_status, created_at)
            OUTPUT INSERTED.id
            VALUES (@employeeCode, @fullName, @email, 'ACTIVE', 'ACTIVE', @utcNow)", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@employeeCode", code);
            cmd.Parameters.AddWithValue("@fullName", name);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@utcNow", utcNow);
            userId = (long)cmd.ExecuteScalar();
        }

        // Insert User_Auth_Accounts
        using (var cmd = new SqlCommand(@"
            INSERT INTO dbo.User_Auth_Accounts (
                user_id, provider_type, provider_subject, password_hash, 
                auth_account_status, failed_attempt_count, must_change_password, 
                security_stamp, created_at, created_by_user_id
            )
            OUTPUT INSERTED.id
            VALUES (
                @userId, 'INTERNAL', @providerSubject, @passwordHash,
                'ACTIVE', 0, 1,
                NEWID(), @utcNow, @createdByUserId
            )", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@providerSubject", email); // provider_subject only, no normalized_provider_subject
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
            cmd.Parameters.AddWithValue("@utcNow", utcNow);
            cmd.Parameters.AddWithValue("@createdByUserId", userId);
            accountId = (long)cmd.ExecuteScalar();
        }

        // Insert Password_History (FK is account_id)
        using (var cmd = new SqlCommand(@"
            INSERT INTO dbo.Password_History (account_id, password_hash, created_at)
            VALUES (@accountId, @passwordHash, @utcNow)", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
            cmd.Parameters.AddWithValue("@utcNow", utcNow);
            cmd.ExecuteNonQuery();
        }

        // Create or get ADMIN_SECURITY group
        using (var cmd = new SqlCommand("SELECT id FROM dbo.Admin_Groups WHERE group_code = 'ADMIN_SECURITY'", connection, transaction))
        {
            var existingGroupId = cmd.ExecuteScalar();
            if (existingGroupId != null)
            {
                adminGroupId = (long)existingGroupId;
            }
            else
            {
                using (var insertCmd = new SqlCommand(@"
                    INSERT INTO dbo.Admin_Groups (group_code, name, scope_type, is_active, created_at, created_by_user_id)
                    OUTPUT INSERTED.id
                    VALUES ('ADMIN_SECURITY', 'Security Administration', 'GLOBAL', 1, @utcNow, @userId)", connection, transaction))
                {
                    insertCmd.Parameters.AddWithValue("@utcNow", utcNow);
                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    adminGroupId = (long)insertCmd.ExecuteScalar();
                }
            }
        }

        // Assign SECURITY_ADMIN_MANAGE to the group (if not already assigned)
        using (var cmd = new SqlCommand(@"
            IF NOT EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions WHERE admin_group_id = @adminGroupId AND permission_code = 'SECURITY_ADMIN_MANAGE')
            BEGIN
                INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code, created_at, created_by_user_id)
                VALUES (@adminGroupId, 'SECURITY_ADMIN_MANAGE', @utcNow, @userId)
            END", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@adminGroupId", adminGroupId);
            cmd.Parameters.AddWithValue("@utcNow", utcNow);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.ExecuteNonQuery();
        }

        // Assign user to the group
        using (var cmd = new SqlCommand(@"
            INSERT INTO dbo.User_Admin_Group_Assignments (user_id, admin_group_id, assignment_status, effective_from, created_at, created_by_user_id)
            VALUES (@userId, @adminGroupId, 'ACTIVE', @utcNow, @utcNow, @userId)", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@adminGroupId", adminGroupId);
            cmd.Parameters.AddWithValue("@utcNow", utcNow);
            cmd.ExecuteNonQuery();
        }

        // Update Security_Bootstrap_State
        using (var cmd = new SqlCommand(@"
            UPDATE dbo.Security_Bootstrap_State
            SET is_bootstrapped = 1,
                bootstrapped_at = @utcNow,
                bootstrapped_by_user_id = @userId
            WHERE id = 1", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@utcNow", utcNow);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.ExecuteNonQuery();
        }

        // Emit BOOTSTRAP_ADMIN_CREATED success audit event transaction-safely
        var correlationId = Guid.NewGuid();
        var afterStateJson = $@"{{""user_id"":{userId},""employee_code"":""{code.Replace("\"", "\\\"")}"",""admin_group_id"":{adminGroupId}}}";

        using (var cmd = new SqlCommand(@"
            INSERT INTO dbo.Security_Audit_Events (
                actor_user_id, acting_as_user_id, target_user_id, company_id,
                event_code, entity_type, entity_id, changed_fields, before_state_json, after_state_json,
                reason, correlation_id, request_metadata, outcome, policy_version
            )
            VALUES (
                @userId, NULL, @userId, NULL,
                'BOOTSTRAP_ADMIN_CREATED', 'BOOTSTRAP', '1', NULL, NULL, @afterStateJson,
                'Initial System Administrator Bootstrap', @correlationId, NULL, 'SUCCESS', 1
            )", connection, transaction))
        {
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@afterStateJson", afterStateJson);
            cmd.Parameters.AddWithValue("@correlationId", correlationId);
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}
