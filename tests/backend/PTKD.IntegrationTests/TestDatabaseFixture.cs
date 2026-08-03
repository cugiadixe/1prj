using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace PTKD.IntegrationTests;

public sealed class TestDatabaseFixture : IDisposable
{
    private static readonly object ResetLock = new();

    private static readonly HashSet<string> KnownTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "SchemaVersions",
        "Users",
        "Companies",
        "Departments",
        "User_Company_Assignments",
        "User_Department_Assignments",
        "Employment_Histories",
        "User_Auth_Accounts",
        "Password_History",
        "Refresh_Tokens",
        "Permissions",
        "Roles",
        "Role_Permissions",
        "Department_Permissions",
        "User_Role_Assignments",
        "User_Individual_Permissions",
        "Admin_Groups",
        "Admin_Group_Permissions",
        "User_Admin_Group_Assignments",
        "Authorization_Policy_State",
        "Security_Bootstrap_State",
        "Security_Audit_Events",
        "Profiles",
        "Customers",
        "Customer_Company_Contexts",
        "Business_Process_Catalog",
        "Workflow_Definitions",
        "Workflow_Definition_Versions",
        "Workflow_Steps",
        "Workflow_Step_Approver_Rules",
        "Workflow_Conditions",
        "Workflow_Bindings",
        "Workflow_Instances",
        "Workflow_Instance_Steps",
        "Workflow_Instance_Step_Assignees",
        "Workflow_Actions",
        "Customer_Change_Requests",
        "Customer_Merge_Requests",
        "Customer_Merge_Request_Candidates",
        "Customer_Merge_History"
    };

    public TestDatabaseFixture()
    {
        ConnectionString = TestDatabaseSafety.ResolveConnectionString();
        RepositoryRoot = FindRepositoryRoot();
        ResetToV0002();
    }

    public string ConnectionString { get; }

    public string RepositoryRoot { get; }

    public string LastVerifiedDatabaseName { get; private set; } = string.Empty;

    public void ResetToV0002()
    {
        lock (ResetLock)
        {
            using var connection = OpenVerifiedConnection();
            RefuseUnknownTables(connection);
            DropKnownSchema(connection);

            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteBatches(ReadMigration("V0001__create_schema_versions.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0001', 'V0001__create_schema_versions.sql', 'APPLIED');");

                ExecuteBatches(ReadMigration("V0002__create_organization_schema.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0002', 'V0002__create_organization_schema.sql', 'APPLIED');");

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public void ResetToEmpty()
    {
        lock (ResetLock)
        {
            using var connection = OpenVerifiedConnection();
            RefuseUnknownTables(connection);
            DropKnownSchema(connection);
        }
    }

    public void ResetToV0003()
    {
        lock (ResetLock)
        {
            ResetToV0002();
            using var connection = OpenVerifiedConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteBatches(ReadMigration("V0003__create_security_schema.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0003', 'V0003__create_security_schema.sql', 'APPLIED');");
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public void ResetToV0004()
    {
        lock (ResetLock)
        {
            ResetToV0003();
            using var connection = OpenVerifiedConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteBatches(ReadMigration("V0004__seed_security_admin_manage_permission.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0004', 'V0004__seed_security_admin_manage_permission.sql', 'APPLIED');");
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public void ResetToV0005()
    {
        lock (ResetLock)
        {
            ResetToV0004();
            using var connection = OpenVerifiedConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteBatches(ReadMigration("V0005__create_customer_schema.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0005', 'V0005__create_customer_schema.sql', 'APPLIED');");
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public void ResetToV0006()
    {
        lock (ResetLock)
        {
            ResetToV0005();
            using var connection = OpenVerifiedConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteBatches(ReadMigration("V0006__create_workflow_schema.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0006', 'V0006__create_workflow_schema.sql', 'APPLIED');");
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public void ResetToV0007()
    {
        lock (ResetLock)
        {
            ResetToV0006();
            using var connection = OpenVerifiedConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteBatches(ReadMigration("V0007__create_customer_change_request.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0007', 'V0007__create_customer_change_request.sql', 'APPLIED');");
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public void ResetToV0008()
    {
        lock (ResetLock)
        {
            ResetToV0007();
            using var connection = OpenVerifiedConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteBatches(ReadMigration("V0008__harden_workflow_runtime.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0008', 'V0008__harden_workflow_runtime.sql', 'APPLIED');");
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public void ResetToV0009()
    {
        lock (ResetLock)
        {
            ResetToV0008();
            using var connection = OpenVerifiedConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteBatches(ReadMigration("V0009__add_customer_change_request_target_fields.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0009', 'V0009__add_customer_change_request_target_fields.sql', 'APPLIED');");
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public void ResetToV0010()
    {
        lock (ResetLock)
        {
            ResetToV0009();
            using var connection = OpenVerifiedConnection();
            using var transaction = connection.BeginTransaction();
            try
            {
                ExecuteBatches(ReadMigration("V0010__customer_merge_backend_data_foundation.sql"), connection, transaction);
                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status) VALUES ('V0010', 'V0010__customer_merge_backend_data_foundation.sql', 'APPLIED');");
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public SqlConnection OpenVerifiedConnection()
    {
        var connection = TestDatabaseSafety.OpenVerifiedConnection(ConnectionString);
        LastVerifiedDatabaseName = TestDatabaseSafety.VerifyOpenConnection(connection);
        return connection;
    }

    public string ReadMigration(string fileName) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, "database", "migrations", fileName));

    public string ReadRollback(string fileName) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, "database", "rollbacks", fileName));

    public static void ExecuteBatches(
        string sql,
        SqlConnection connection,
        SqlTransaction? transaction = null)
    {
        var batches = Regex.Split(
            sql,
            @"^\s*GO\s*;?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        foreach (var batch in batches.Where(batch => !string.IsNullOrWhiteSpace(batch)))
        {
            using var command = new SqlCommand(batch, connection, transaction)
            {
                CommandTimeout = 60
            };
            command.ExecuteNonQuery();
        }
    }

    private static void ExecuteNonQuery(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        command.ExecuteNonQuery();
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "database", "migrations"))
                    && Directory.Exists(Path.Combine(directory.FullName, "src", "backend")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the PTKD ERP repository root.");
    }

    private static void RefuseUnknownTables(SqlConnection connection)
    {
        using var command = new SqlCommand(
            "SELECT name FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo');",
            connection);
        using var reader = command.ExecuteReader();
        var unexpected = new List<string>();

        while (reader.Read())
        {
            var tableName = reader.GetString(0);
            if (!KnownTables.Contains(tableName))
            {
                unexpected.Add(tableName);
            }
        }

        if (unexpected.Count > 0)
        {
            throw new InvalidOperationException(
                $"Unexpected dbo tables in {TestDatabaseSafety.ApprovedDatabaseName}: " +
                string.Join(", ", unexpected.OrderBy(name => name)));
        }
    }

    private static void DropKnownSchema(SqlConnection connection)
    {
        const string sql = """
            IF DATABASE_PRINCIPAL_ID(N'PTKD_Security_Audit_Runtime') IS NOT NULL
            BEGIN
                DECLARE @drop_members nvarchar(max) = N'';
                SELECT @drop_members = @drop_members
                    + N'ALTER ROLE PTKD_Security_Audit_Runtime DROP MEMBER ' + QUOTENAME(member_principal.name) + N';'
                FROM sys.database_role_members AS membership
                INNER JOIN sys.database_principals AS role_principal
                    ON role_principal.principal_id = membership.role_principal_id
                INNER JOIN sys.database_principals AS member_principal
                    ON member_principal.principal_id = membership.member_principal_id
                WHERE role_principal.name = N'PTKD_Security_Audit_Runtime';

                IF @drop_members <> N''
                    EXEC sys.sp_executesql @drop_members;
            END;

            IF USER_ID(N'PTKD_SecurityAuditRuntime_Test') IS NOT NULL
                DROP USER PTKD_SecurityAuditRuntime_Test;

            DROP TRIGGER IF EXISTS dbo.TR_User_Admin_Group_Assignments_PreventOverlap;
            DROP TRIGGER IF EXISTS dbo.TR_User_Individual_Permissions_PreventOverlap;
            DROP TRIGGER IF EXISTS dbo.TR_User_Role_Assignments_PreventOverlap;
            DROP TRIGGER IF EXISTS dbo.TR_Password_History_AppendOnly;
            DROP TRIGGER IF EXISTS dbo.TR_Permissions_PreventCodeChange;
            DROP TRIGGER IF EXISTS dbo.TR_Permissions_PreventDelete;
            DROP TRIGGER IF EXISTS dbo.TR_Security_Audit_Events_AppendOnly;
            DROP TRIGGER IF EXISTS dbo.TR_Security_Audit_Events_PreventUpdateDelete;
            DROP VIEW IF EXISTS dbo.vw_SECURITY_AUDIT_VIEW;

            IF DATABASE_PRINCIPAL_ID(N'PTKD_Security_Audit_Runtime') IS NOT NULL
                DROP ROLE PTKD_Security_Audit_Runtime;



            IF OBJECT_ID(N'dbo.FK_EmploymentHistories_created_by', N'F') IS NOT NULL
                ALTER TABLE dbo.Employment_Histories DROP CONSTRAINT FK_EmploymentHistories_created_by;
            IF OBJECT_ID(N'dbo.FK_UserDepartmentAssignments_created_by', N'F') IS NOT NULL
                ALTER TABLE dbo.User_Department_Assignments DROP CONSTRAINT FK_UserDepartmentAssignments_created_by;
            IF OBJECT_ID(N'dbo.FK_UserDepartmentAssignments_updated_by', N'F') IS NOT NULL
                ALTER TABLE dbo.User_Department_Assignments DROP CONSTRAINT FK_UserDepartmentAssignments_updated_by;
            IF OBJECT_ID(N'dbo.FK_UserCompanyAssignments_created_by', N'F') IS NOT NULL
                ALTER TABLE dbo.User_Company_Assignments DROP CONSTRAINT FK_UserCompanyAssignments_created_by;
            IF OBJECT_ID(N'dbo.FK_UserCompanyAssignments_updated_by', N'F') IS NOT NULL
                ALTER TABLE dbo.User_Company_Assignments DROP CONSTRAINT FK_UserCompanyAssignments_updated_by;
            IF OBJECT_ID(N'dbo.FK_Departments_created_by', N'F') IS NOT NULL
                ALTER TABLE dbo.Departments DROP CONSTRAINT FK_Departments_created_by;
            IF OBJECT_ID(N'dbo.FK_Departments_updated_by', N'F') IS NOT NULL
                ALTER TABLE dbo.Departments DROP CONSTRAINT FK_Departments_updated_by;
            IF OBJECT_ID(N'dbo.FK_Companies_created_by', N'F') IS NOT NULL
                ALTER TABLE dbo.Companies DROP CONSTRAINT FK_Companies_created_by;
            IF OBJECT_ID(N'dbo.FK_Companies_updated_by', N'F') IS NOT NULL
                ALTER TABLE dbo.Companies DROP CONSTRAINT FK_Companies_updated_by;
            IF OBJECT_ID(N'dbo.FK_Users_created_by', N'F') IS NOT NULL
                ALTER TABLE dbo.Users DROP CONSTRAINT FK_Users_created_by;
            IF OBJECT_ID(N'dbo.FK_Users_updated_by', N'F') IS NOT NULL
                ALTER TABLE dbo.Users DROP CONSTRAINT FK_Users_updated_by;

            DROP TABLE IF EXISTS dbo.Security_Audit_Events;
            DROP TABLE IF EXISTS dbo.Security_Bootstrap_State;
            DROP TABLE IF EXISTS dbo.Authorization_Policy_State;
            DROP TABLE IF EXISTS dbo.User_Admin_Group_Assignments;
            DROP TABLE IF EXISTS dbo.Admin_Group_Permissions;
            DROP TABLE IF EXISTS dbo.Admin_Groups;
            DROP TABLE IF EXISTS dbo.User_Individual_Permissions;
            DROP TABLE IF EXISTS dbo.User_Role_Assignments;
            DROP TABLE IF EXISTS dbo.Department_Permissions;
            DROP TABLE IF EXISTS dbo.Role_Permissions;
            DROP TABLE IF EXISTS dbo.Roles;
            DROP TABLE IF EXISTS dbo.Permissions;
            DROP TABLE IF EXISTS dbo.Refresh_Tokens;
            DROP TABLE IF EXISTS dbo.Password_History;
            DROP TABLE IF EXISTS dbo.User_Auth_Sessions;
            DROP TABLE IF EXISTS dbo.User_Auth_Accounts;

            DROP TABLE IF EXISTS dbo.Customer_Merge_History;
            DROP TABLE IF EXISTS dbo.Customer_Merge_Request_Candidates;
            DROP TABLE IF EXISTS dbo.Customer_Merge_Requests;
            DROP TABLE IF EXISTS dbo.Customer_Change_Requests;
            DROP TABLE IF EXISTS dbo.Workflow_Actions;
            DROP TABLE IF EXISTS dbo.Workflow_Instance_Step_Assignees;
            DROP TABLE IF EXISTS dbo.Workflow_Instance_Steps;
            DROP TABLE IF EXISTS dbo.Workflow_Instances;
            DROP TABLE IF EXISTS dbo.Workflow_Bindings;
            DROP TABLE IF EXISTS dbo.Workflow_Conditions;
            DROP TABLE IF EXISTS dbo.Workflow_Step_Approver_Rules;
            DROP TABLE IF EXISTS dbo.Workflow_Steps;
            DROP TABLE IF EXISTS dbo.Workflow_Definition_Versions;
            DROP TABLE IF EXISTS dbo.Workflow_Definitions;
            DROP TABLE IF EXISTS dbo.Business_Process_Catalog;

            DROP TABLE IF EXISTS dbo.Customer_Company_Contexts;
            DROP TABLE IF EXISTS dbo.Customers;
            DROP TABLE IF EXISTS dbo.Profiles;

            DROP TABLE IF EXISTS dbo.Employment_Histories;
            DROP TABLE IF EXISTS dbo.User_Department_Assignments;
            DROP TABLE IF EXISTS dbo.User_Company_Assignments;
            DROP TABLE IF EXISTS dbo.Departments;
            DROP TABLE IF EXISTS dbo.Companies;
            DROP TABLE IF EXISTS dbo.Users;
            DROP TABLE IF EXISTS dbo.SchemaVersions;
            """;

        using var transaction = connection.BeginTransaction();
        try
        {
            using var command = new SqlCommand(sql, connection, transaction)
            {
                CommandTimeout = 60
            };
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Dispose()
    {
    }
}

[CollectionDefinition("Sequential", DisableParallelization = true)]
public sealed class SequentialCollection : ICollectionFixture<TestDatabaseFixture>
{
}
