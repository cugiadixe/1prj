using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace PTKD.IntegrationTests;

[Collection("Sequential")]
public sealed class SecuritySchemaTests : IDisposable
{
    private static readonly string[] ExpectedTables =
    [
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
        "Security_Audit_Events"
    ];

    private static readonly string[] ExpectedPermissionCodes =
    [
        "CUSTOMER_CREATE_FINAL",
        "CUSTOMER_MASTER_UPDATE",
        "CUSTOMER_MERGE_EXECUTE",
        "CUSTOMER_MERGE_REQUEST_ADMIN_VIEW",
        "CUSTOMER_MERGE_REQUEST_CREATE",
        "CUSTOMER_MERGE_REQUEST_VIEW",
        "CUSTOMER_VIEW_BASIC",
        "CUSTOMER_VIEW_SENSITIVE",
        "ORGANIZATION_COMPANY_MANAGE",
        "ORGANIZATION_COMPANY_VIEW",
        "ORGANIZATION_DEPARTMENT_MANAGE",
        "ORGANIZATION_DEPARTMENT_VIEW",
        "SECURITY_ACCOUNT_MANAGE",
        "SECURITY_ADMIN_GROUP_MANAGE",
        "SECURITY_ADMIN_GROUP_VIEW",
        "SECURITY_ADMIN_MANAGE",
        "SECURITY_ASSIGNMENT_MANAGE",
        "SECURITY_AUDIT_VIEW",
        "SECURITY_PERMISSION_MANAGE",
        "SECURITY_PERMISSION_VIEW",
        "SECURITY_ROLE_MANAGE",
        "SECURITY_ROLE_VIEW",
        "SECURITY_USER_MANAGE",
        "SECURITY_USER_VIEW",
        "WORKFLOW_AUDIT_VIEW",
        "WORKFLOW_BIND_PROCESS",
        "WORKFLOW_CONFIG_MANAGE",
        "WORKFLOW_PUBLISH",
        "WORKFLOW_REASSIGN_PENDING",
        "WORKFLOW_VIEW"
    ];

    private readonly TestDatabaseFixture _fixture;

    public SecuritySchemaTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Migrator_AppliesV0001V0002V0003ExactlyOnce_AndRecordsV0003()
    {
        _fixture.ResetToEmpty();

        var firstOutput = ExecuteDbMigrator();
        Assert.Contains("Applied V0001", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0002", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Applied V0003", firstOutput, StringComparison.Ordinal);

        using (var connection = _fixture.OpenVerifiedConnection())
        {
            Assert.Equal(ExpectedTables.Length, CountExpectedTables(connection));
            Assert.Equal(1, CountVersion(connection, "V0003"));

            using var command = new SqlCommand(
                "SELECT ScriptName, Status FROM dbo.SchemaVersions WHERE Version = 'V0003';",
                connection);
            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal("V0003__create_security_schema.sql", reader.GetString(0));
            Assert.Equal("APPLIED", reader.GetString(1));
            Assert.False(reader.Read());
        }

        var secondOutput = ExecuteDbMigrator();
        Assert.Contains("Skipping V0003", secondOutput, StringComparison.Ordinal);
        using var secondConnection = _fixture.OpenVerifiedConnection();
        Assert.Equal(1, CountVersion(secondConnection, "V0003"));
    }

    [Fact]
    public void Migrator_FailedMigration_IsAtomic()
    {
        _fixture.ResetToEmpty();
        ExecuteDbMigrator();

        var badMigrationPath = Path.Combine(
            _fixture.RepositoryRoot,
            "database",
            "migrations",
            "V9998__security_test_atomicity_failure.sql");
        Assert.False(File.Exists(badMigrationPath), $"Unexpected existing test migration: {badMigrationPath}");

        File.WriteAllText(
            badMigrationPath,
            "CREATE TABLE dbo.SecurityAtomicityProbe (id int NOT NULL);\nGO\n" +
            "SELECT * FROM dbo.SecurityAtomicityMissingTable;\nGO\n");

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(ExecuteDbMigrator);
            Assert.Contains("transaction rolled back", exception.Message, StringComparison.OrdinalIgnoreCase);

            using var connection = _fixture.OpenVerifiedConnection();
            Assert.Equal(0, ScalarInt(
                connection,
                "SELECT COUNT(*) FROM sys.tables WHERE name = N'SecurityAtomicityProbe';"));
            Assert.Equal(0, CountVersion(connection, "V9998"));
            Assert.Equal(1, CountVersion(connection, "V0003"));
        }
        finally
        {
            File.Delete(badMigrationPath);
        }
    }

    [Fact]
    public void Schema_HasExpectedTablesNamedObjectsRowVersionsAndNoCascadeDelete()
    {
        using var connection = OpenKnownV0003Baseline();

        Assert.Equal(ExpectedTables.Length, CountExpectedTables(connection));
        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*)
            FROM sys.key_constraints AS constraint_object
            INNER JOIN sys.tables AS table_object ON table_object.object_id = constraint_object.parent_object_id
            WHERE table_object.name IN (
                'User_Auth_Accounts', 'Password_History', 'Refresh_Tokens', 'Permissions', 'Roles',
                'Role_Permissions', 'Department_Permissions', 'User_Role_Assignments',
                'User_Individual_Permissions', 'Admin_Groups', 'Admin_Group_Permissions',
                'User_Admin_Group_Assignments', 'Authorization_Policy_State',
                'Security_Bootstrap_State', 'Security_Audit_Events')
              AND constraint_object.is_system_named = 1;
            """));
        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*)
            FROM sys.check_constraints AS constraint_object
            INNER JOIN sys.tables AS table_object ON table_object.object_id = constraint_object.parent_object_id
            WHERE table_object.name IN (
                'User_Auth_Accounts', 'Refresh_Tokens', 'Permissions', 'Roles',
                'User_Role_Assignments', 'User_Individual_Permissions', 'Admin_Groups',
                'User_Admin_Group_Assignments', 'Authorization_Policy_State',
                'Security_Bootstrap_State', 'Security_Audit_Events')
              AND constraint_object.is_system_named = 1;
            """));
        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*)
            FROM sys.default_constraints AS constraint_object
            INNER JOIN sys.tables AS table_object ON table_object.object_id = constraint_object.parent_object_id
            WHERE table_object.name IN (
                'User_Auth_Accounts', 'Password_History', 'Refresh_Tokens', 'Permissions', 'Roles',
                'Role_Permissions', 'Department_Permissions', 'User_Role_Assignments',
                'User_Individual_Permissions', 'Admin_Groups', 'Admin_Group_Permissions',
                'User_Admin_Group_Assignments', 'Authorization_Policy_State',
                'Security_Bootstrap_State', 'Security_Audit_Events')
              AND constraint_object.is_system_named = 1;
            """));
        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*)
            FROM sys.foreign_keys
            WHERE delete_referential_action <> 0
              AND parent_object_id IN (
                SELECT object_id FROM sys.tables
                WHERE name IN (
                    'User_Auth_Accounts', 'Password_History', 'Refresh_Tokens', 'Roles',
                    'Role_Permissions', 'Department_Permissions', 'User_Role_Assignments',
                    'User_Individual_Permissions', 'Admin_Groups', 'Admin_Group_Permissions',
                    'User_Admin_Group_Assignments', 'Authorization_Policy_State',
                    'Security_Bootstrap_State', 'Security_Audit_Events'));
            """));

        var rowVersionTables = QueryStrings(connection, """
            SELECT table_object.name
            FROM sys.columns AS column_object
            INNER JOIN sys.tables AS table_object ON table_object.object_id = column_object.object_id
            WHERE column_object.name = 'row_version'
              AND TYPE_NAME(column_object.user_type_id) = 'timestamp'
              AND table_object.name IN (
                  'User_Auth_Accounts', 'Refresh_Tokens', 'Permissions', 'Roles',
                  'User_Role_Assignments', 'User_Individual_Permissions', 'Admin_Groups',
                  'User_Admin_Group_Assignments', 'Authorization_Policy_State', 'Security_Bootstrap_State')
            ORDER BY table_object.name;
            """);
        Assert.Equal(
            new[]
            {
                "Admin_Groups", "Authorization_Policy_State", "Permissions", "Refresh_Tokens",
                "Roles", "Security_Bootstrap_State", "User_Admin_Group_Assignments",
                "User_Auth_Accounts", "User_Individual_Permissions", "User_Role_Assignments"
            },
            rowVersionTables);

        foreach (var triggerName in new[]
        {
            "TR_Password_History_AppendOnly",
            "TR_Permissions_PreventDelete",
            "TR_Permissions_PreventCodeChange",
            "TR_User_Role_Assignments_PreventOverlap",
            "TR_User_Individual_Permissions_PreventOverlap",
            "TR_User_Admin_Group_Assignments_PreventOverlap",
            "TR_Security_Audit_Events_AppendOnly"
        })
        {
            Assert.Equal(1, ScalarInt(
                connection,
                "SELECT COUNT(*) FROM sys.triggers WHERE name = @name;",
                new SqlParameter("@name", triggerName)));
        }

        foreach (var indexName in new[]
        {
            "IX_PasswordHistory_Account_CreatedAt",
            "UQ_UserRoleAssignments_CurrentActive",
            "IX_UserRoleAssignments_OverlapLookup",
            "UQ_UserIndividualPermissions_CurrentActiveCompany",
            "UQ_UserIndividualPermissions_CurrentActiveGlobal",
            "IX_UserIndividualPermissions_OverlapLookup",
            "UQ_UserAdminGroupAssignments_CurrentActive",
            "IX_UserAdminGroupAssignments_OverlapLookup"
        })
        {
            Assert.Equal(1, ScalarInt(
                connection,
                "SELECT COUNT(*) FROM sys.indexes WHERE name = @name;",
                new SqlParameter("@name", indexName)));
        }
    }

    [Fact]
    public void Permissions_UseNaturalPrimaryKey_AndExactImmutableSeedCatalog()
    {
        _fixture.ResetToEmpty();
        ExecuteDbMigrator();
        using var connection = _fixture.OpenVerifiedConnection();

        using (var command = new SqlCommand("""
            SELECT TYPE_NAME(column_object.user_type_id), column_object.max_length
            FROM sys.indexes AS index_object
            INNER JOIN sys.index_columns AS index_column
                ON index_column.object_id = index_object.object_id
                AND index_column.index_id = index_object.index_id
            INNER JOIN sys.columns AS column_object
                ON column_object.object_id = index_column.object_id
                AND column_object.column_id = index_column.column_id
            WHERE index_object.object_id = OBJECT_ID(N'dbo.Permissions')
              AND index_object.is_primary_key = 1
              AND index_column.key_ordinal = 1;
            """, connection))
        using (var reader = command.ExecuteReader())
        {
            Assert.True(reader.Read());
            Assert.Equal("varchar", reader.GetString(0));
            Assert.Equal(100, reader.GetInt16(1));
            Assert.False(reader.Read());
        }

        var actualCodes = QueryStrings(
            connection,
            "SELECT permission_code FROM dbo.Permissions ORDER BY permission_code;");
        Assert.Equal(ExpectedPermissionCodes, actualCodes);

        var entityException = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection, """
            INSERT INTO dbo.Permissions
                (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active)
            VALUES ('TEST_ENTITY', 'TEST', 'TEST', 'ENTITY', 0, 0, 0, 1);
            """));
        Assert.Contains("CK_Permissions_DataScope", entityException.Message, StringComparison.Ordinal);

        var updateCodeException = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection, """
            UPDATE dbo.Permissions
            SET permission_code = 'SECURITY_ROLE_VIEW_RENAMED'
            WHERE permission_code = 'SECURITY_ROLE_VIEW';
            """));
        Assert.Contains("immutable", updateCodeException.Message, StringComparison.OrdinalIgnoreCase);

        var deleteException = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection, """
            DELETE FROM dbo.Permissions WHERE permission_code = 'SECURITY_ROLE_VIEW';
            """));
        Assert.Contains("may not be deleted", deleteException.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Permissions_V0004_ContainsSecurityAdminManage()
    {
        _fixture.ResetToEmpty();
        ExecuteDbMigrator();

        using var connection = _fixture.OpenVerifiedConnection();
        using var command = new SqlCommand("""
            SELECT permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active
            FROM dbo.Permissions
            WHERE permission_code = 'SECURITY_ADMIN_MANAGE';
            """, connection);

        using var reader = command.ExecuteReader();
        Assert.True(reader.Read(), "SECURITY_ADMIN_MANAGE row was not found.");

        Assert.Equal("SECURITY_ADMIN_MANAGE", reader.GetString(0));
        Assert.Equal("SECURITY", reader.GetString(1));
        Assert.Equal("ADMIN_MANAGE", reader.GetString(2));
        Assert.Equal("GLOBAL", reader.GetString(3));
        Assert.True(reader.GetBoolean(4)); // is_sensitive
        Assert.True(reader.GetBoolean(5)); // requires_reason
        Assert.False(reader.GetBoolean(6)); // is_delegable
        Assert.True(reader.GetBoolean(7)); // is_active

        Assert.False(reader.Read(), "Multiple SECURITY_ADMIN_MANAGE rows found.");
    }

    [Fact]
    public void RoleAdminGroupAndIndividualPermission_ScopeAndStableCodeConstraintsAreEnforced()
    {
        using var connection = OpenKnownV0003Baseline();
        var companyId = InsertCompany(connection);
        var userId = InsertUser(connection);

        AssertSqlConstraint(connection,
            $"INSERT INTO dbo.Roles (role_code, name, scope_type, company_id) VALUES ('BAD_ROLE_GLOBAL', N'Bad', 'GLOBAL', {companyId});",
            "CK_Roles_ScopeCompany");
        AssertSqlConstraint(connection,
            "INSERT INTO dbo.Roles (role_code, name, scope_type, company_id) VALUES ('BAD_ROLE_COMPANY', N'Bad', 'COMPANY', NULL);",
            "CK_Roles_ScopeCompany");
        ExecuteNonQuery(connection,
            "INSERT INTO dbo.Roles (role_code, name, scope_type, company_id) VALUES ('ROLE_STABLE', N'Role', 'GLOBAL', NULL);");
        AssertSqlConstraint(connection,
            "INSERT INTO dbo.Roles (role_code, name, scope_type, company_id) VALUES ('ROLE_STABLE', N'Duplicate', 'GLOBAL', NULL);",
            "UQ_Roles_RoleCode");

        AssertSqlConstraint(connection,
            $"INSERT INTO dbo.Admin_Groups (group_code, name, scope_type, company_id) VALUES ('BAD_GROUP_GLOBAL', N'Bad', 'GLOBAL', {companyId});",
            "CK_AdminGroups_ScopeCompany");
        AssertSqlConstraint(connection,
            "INSERT INTO dbo.Admin_Groups (group_code, name, scope_type, company_id) VALUES ('BAD_GROUP_COMPANY', N'Bad', 'COMPANY', NULL);",
            "CK_AdminGroups_ScopeCompany");
        ExecuteNonQuery(connection,
            "INSERT INTO dbo.Admin_Groups (group_code, name, scope_type, company_id) VALUES ('GROUP_STABLE', N'Group', 'GLOBAL', NULL);");
        AssertSqlConstraint(connection,
            "INSERT INTO dbo.Admin_Groups (group_code, name, scope_type, company_id) VALUES ('GROUP_STABLE', N'Duplicate', 'GLOBAL', NULL);",
            "UQ_AdminGroups_GroupCode");

        AssertSqlConstraint(connection,
            $"""
            INSERT INTO dbo.User_Individual_Permissions
                (user_id, permission_code, scope_type, company_id, grant_type, effective_from)
            VALUES ({userId}, 'SECURITY_USER_VIEW', 'GLOBAL', {companyId}, 'ALLOW', '2030-01-01');
            """,
            "CK_UserIndividualPermissions_ScopeCompany");
        AssertSqlConstraint(connection,
            $"""
            INSERT INTO dbo.User_Individual_Permissions
                (user_id, permission_code, scope_type, company_id, grant_type, effective_from)
            VALUES ({userId}, 'SECURITY_USER_VIEW', 'COMPANY', NULL, 'ALLOW', '2030-01-01');
            """,
            "CK_UserIndividualPermissions_ScopeCompany");

        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*)
            FROM sys.columns
            WHERE object_id IN (
                OBJECT_ID(N'dbo.User_Role_Assignments'),
                OBJECT_ID(N'dbo.User_Admin_Group_Assignments'))
              AND name = 'company_id';
            """));
    }

    [Fact]
    public void SingletonStatesAndMutableRows_EnforceSingletonsAndChangeRowVersion()
    {
        using var connection = OpenKnownV0003Baseline();

        AssertSqlConstraint(connection,
            "INSERT INTO dbo.Authorization_Policy_State (id, policy_version) VALUES (2, 1);",
            "CK_AuthorizationPolicyState_Singleton");
        AssertSqlConstraint(connection,
            "INSERT INTO dbo.Security_Bootstrap_State (id) VALUES (2);",
            "CK_SecurityBootstrapState_Singleton");

        var roleId = InsertRole(connection);
        var before = ScalarBytes(
            connection,
            $"SELECT row_version FROM dbo.Roles WHERE id = {roleId};");
        var after = ScalarBytes(
            connection,
            $"UPDATE dbo.Roles SET name = N'Updated Role' OUTPUT inserted.row_version WHERE id = {roleId};");
        Assert.NotEqual(before, after);

        var userId = InsertUser(connection);
        ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.User_Role_Assignments
                (user_id, role_id, assignment_status, effective_from, effective_to)
            VALUES ({userId}, {roleId}, 'SCHEDULED', '2030-01-01', '2030-02-01');
            """);
        var assignmentBefore = ScalarBytes(
            connection,
            "SELECT TOP (1) row_version FROM dbo.User_Role_Assignments;");
        ExecuteNonQuery(connection, """
            UPDATE dbo.User_Role_Assignments
            SET updated_at = SYSUTCDATETIME();
            """);
        var assignmentAfter = ScalarBytes(
            connection,
            "SELECT TOP (1) row_version FROM dbo.User_Role_Assignments;");
        Assert.NotEqual(assignmentBefore, assignmentAfter);
    }

    [Fact]
    public void AuthenticationStorage_EnforcesIdentityHashAndHistoryRequirements()
    {
        using var connection = OpenKnownV0003Baseline();
        var userId = InsertUser(connection);

        var accountId = ScalarLong(connection, $"""
            INSERT INTO dbo.User_Auth_Accounts
                (user_id, provider_type, provider_subject, password_hash)
            OUTPUT inserted.id
            VALUES ({userId}, 'EXTERNAL', 'external-subject', NULL);
            """);
        Assert.True(accountId > 0);
        Assert.Equal(DBNull.Value, Scalar(connection,
            $"SELECT password_hash FROM dbo.User_Auth_Accounts WHERE id = {accountId};"));

        AssertSqlConstraint(connection, $"""
            INSERT INTO dbo.User_Auth_Accounts
                (user_id, provider_type, provider_subject, password_hash)
            VALUES ({userId}, 'EXTERNAL', 'external-subject', NULL);
            """, "UQ_UserAuthAccounts_ProviderSubject");

        ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.Password_History (account_id, password_hash)
            VALUES ({accountId}, 'hash-one');
            """);
        Assert.Throws<SqlException>(() => ExecuteNonQuery(connection,
            "UPDATE dbo.Password_History SET password_hash = 'changed';"));
        Assert.Throws<SqlException>(() => ExecuteNonQuery(connection,
            "DELETE FROM dbo.Password_History;"));
        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*) FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Password_History') AND name = 'row_version';
            """));

        ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.Refresh_Tokens
                (account_id, token_hash, family_id, session_id, expires_at)
            VALUES
                ({accountId}, REPLICATE('A', 64), NEWID(), NEWID(), DATEADD(day, 7, SYSUTCDATETIME()));
            """);
        AssertSqlConstraint(connection, $"""
            INSERT INTO dbo.Refresh_Tokens
                (account_id, token_hash, family_id, session_id, expires_at)
            VALUES
                ({accountId}, REPLICATE('A', 64), NEWID(), NEWID(), DATEADD(day, 7, SYSUTCDATETIME()));
            """, "UQ_RefreshTokens_TokenHash");

        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*)
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Refresh_Tokens')
              AND name IN ('token', 'raw_token', 'refresh_token', 'token_value');
            """));
        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*)
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.User_Auth_Accounts')
              AND name = 'normalized_provider_subject';
            """));

        var historyIndexColumns = QueryStrings(connection, """
            SELECT column_object.name
            FROM sys.indexes AS index_object
            INNER JOIN sys.index_columns AS index_column
                ON index_column.object_id = index_object.object_id
                AND index_column.index_id = index_object.index_id
            INNER JOIN sys.columns AS column_object
                ON column_object.object_id = index_column.object_id
                AND column_object.column_id = index_column.column_id
            WHERE index_object.object_id = OBJECT_ID(N'dbo.Password_History')
              AND index_object.name = N'IX_PasswordHistory_Account_CreatedAt'
              AND index_column.key_ordinal > 0
            ORDER BY index_column.key_ordinal;
            """);
        Assert.Equal(new[] { "account_id", "created_at", "id" }, historyIndexColumns);
        Assert.Equal(2, ScalarInt(connection, """
            SELECT COUNT(*)
            FROM sys.indexes AS index_object
            INNER JOIN sys.index_columns AS index_column
                ON index_column.object_id = index_object.object_id
                AND index_column.index_id = index_object.index_id
            WHERE index_object.object_id = OBJECT_ID(N'dbo.Password_History')
              AND index_object.name = N'IX_PasswordHistory_Account_CreatedAt'
              AND index_column.is_descending_key = 1;
            """));
    }

    [Fact]
    public void RoleAndAdminGroupAssignments_RejectOverlapsAndAcceptAdjacentScheduledPeriods()
    {
        using var connection = OpenKnownV0003Baseline();
        var userId = InsertUser(connection);
        var roleId = InsertRole(connection);
        var groupId = InsertAdminGroup(connection);

        var roleOverlap = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.User_Role_Assignments
                (user_id, role_id, assignment_status, effective_from, effective_to)
            VALUES
                ({userId}, {roleId}, 'SCHEDULED', '2030-01-01', '2030-03-01'),
                ({userId}, {roleId}, 'SCHEDULED', '2030-02-01', '2030-04-01');
            """));
        Assert.Contains("may not overlap", roleOverlap.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, ScalarInt(connection, "SELECT COUNT(*) FROM dbo.User_Role_Assignments;"));

        ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.User_Role_Assignments
                (user_id, role_id, assignment_status, effective_from, effective_to)
            VALUES
                ({userId}, {roleId}, 'SCHEDULED', '2030-01-01', '2030-02-01'),
                ({userId}, {roleId}, 'SCHEDULED', '2030-02-01', '2030-03-01');
            """);
        Assert.Equal(2, ScalarInt(connection, "SELECT COUNT(*) FROM dbo.User_Role_Assignments;"));

        var groupOverlap = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.User_Admin_Group_Assignments
                (user_id, admin_group_id, assignment_status, effective_from, effective_to)
            VALUES
                ({userId}, {groupId}, 'SCHEDULED', '2030-01-01', '2030-03-01'),
                ({userId}, {groupId}, 'SCHEDULED', '2030-02-01', '2030-04-01');
            """));
        Assert.Contains("may not overlap", groupOverlap.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, ScalarInt(connection, "SELECT COUNT(*) FROM dbo.User_Admin_Group_Assignments;"));

        ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.User_Admin_Group_Assignments
                (user_id, admin_group_id, assignment_status, effective_from, effective_to)
            VALUES
                ({userId}, {groupId}, 'SCHEDULED', '2030-01-01', '2030-02-01'),
                ({userId}, {groupId}, 'SCHEDULED', '2030-02-01', '2030-03-01');
            """);
        Assert.Equal(2, ScalarInt(connection, "SELECT COUNT(*) FROM dbo.User_Admin_Group_Assignments;"));
    }

    [Fact]
    public void IndividualPermissions_AllowDenyCoexist_RejectSameEffectOverlap_AndValidateDates()
    {
        using var connection = OpenKnownV0003Baseline();
        var userId = InsertUser(connection);

        ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.User_Individual_Permissions
                (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, effective_to)
            VALUES
                ({userId}, 'SECURITY_USER_VIEW', 'GLOBAL', NULL, 'ALLOW', 'SCHEDULED', '2030-01-01', '2030-02-01'),
                ({userId}, 'SECURITY_USER_VIEW', 'GLOBAL', NULL, 'DENY',  'SCHEDULED', '2030-01-01', '2030-02-01');
            """);
        Assert.Equal(2, ScalarInt(connection, "SELECT COUNT(*) FROM dbo.User_Individual_Permissions;"));

        var overlap = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.User_Individual_Permissions
                (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, effective_to)
            VALUES
                ({userId}, 'SECURITY_USER_VIEW', 'GLOBAL', NULL, 'ALLOW', 'SCHEDULED', '2030-01-15', '2030-02-15');
            """));
        Assert.Contains("same grant stream", overlap.Message, StringComparison.OrdinalIgnoreCase);

        ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.User_Individual_Permissions
                (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, effective_to)
            VALUES
                ({userId}, 'SECURITY_USER_VIEW', 'GLOBAL', NULL, 'ALLOW', 'SCHEDULED', '2030-02-01', '2030-03-01');
            """);
        Assert.Equal(3, ScalarInt(connection, "SELECT COUNT(*) FROM dbo.User_Individual_Permissions;"));

        AssertSqlConstraint(connection, $"""
            INSERT INTO dbo.User_Individual_Permissions
                (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, effective_to)
            VALUES
                ({userId}, 'SECURITY_ROLE_VIEW', 'GLOBAL', NULL, 'ALLOW', 'SCHEDULED', '2030-02-01', '2030-02-01');
            """, "CK_UserIndividualPermissions_EffectiveDates");
        AssertSqlConstraint(connection, $"""
            INSERT INTO dbo.User_Individual_Permissions
                (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, effective_to)
            VALUES
                ({userId}, 'SECURITY_ROLE_VIEW', 'GLOBAL', NULL, 'GRANT', 'SCHEDULED', '2030-02-01', '2030-03-01');
            """, "CK_UserIndividualPermissions_GrantType");
    }

    [Fact]
    public void AuditRuntimeRole_AllowsSelectInsert_AndDeniesUpdateDeleteAlterTruncate()
    {
        using var connection = OpenKnownV0003Baseline();
        CreateAuditRuntimeTestUser(connection);

        ExecuteAsAuditRuntimeUser(connection, """
            INSERT INTO dbo.Security_Audit_Events
                (event_code, entity_type, correlation_id, outcome)
            VALUES ('TEST_RUNTIME_INSERT', 'SECURITY_TEST', NEWID(), 'SUCCESS');
            """);
        var visibleRows = ExecuteScalarAsAuditRuntimeUser(
            connection,
            "SELECT COUNT(*) FROM dbo.Security_Audit_Events;");
        Assert.Equal(1, Convert.ToInt32(visibleRows));

        var effectivePermissions = ExecutePermissionProbeAsAuditRuntimeUser(connection);
        Assert.Equal(new[] { 1, 1, 0, 0, 0 }, effectivePermissions);

        Assert.Throws<SqlException>(() => ExecuteAsAuditRuntimeUser(connection,
            "UPDATE dbo.Security_Audit_Events SET outcome = 'CHANGED';"));
        Assert.Throws<SqlException>(() => ExecuteAsAuditRuntimeUser(connection,
            "DELETE FROM dbo.Security_Audit_Events;"));
        Assert.Throws<SqlException>(() => ExecuteAsAuditRuntimeUser(connection,
            "ALTER TABLE dbo.Security_Audit_Events ADD unauthorized_column int NULL;"));
        Assert.Throws<SqlException>(() => ExecuteAsAuditRuntimeUser(connection,
            "TRUNCATE TABLE dbo.Security_Audit_Events;"));

        Assert.Equal(1, ScalarInt(connection, "SELECT COUNT(*) FROM dbo.Security_Audit_Events;"));
        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*) FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Security_Audit_Events')
              AND name = N'unauthorized_column';
            """));
    }

    [Fact]
    public void AuditAppendOnlyTrigger_BlocksUpdateAndDeleteForPrivilegedWriter()
    {
        using var connection = OpenKnownV0003Baseline();
        ExecuteNonQuery(connection, """
            INSERT INTO dbo.Security_Audit_Events
                (event_code, entity_type, correlation_id, outcome)
            VALUES
                ('TEST_TRIGGER_1', 'SECURITY_TEST', NEWID(), 'SUCCESS'),
                ('TEST_TRIGGER_2', 'SECURITY_TEST', NEWID(), 'SUCCESS');
            """);

        var updateException = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection,
            "UPDATE dbo.Security_Audit_Events SET outcome = 'CHANGED';"));
        Assert.Contains("append-only", updateException.Message, StringComparison.OrdinalIgnoreCase);
        var deleteException = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection,
            "DELETE FROM dbo.Security_Audit_Events;"));
        Assert.Contains("append-only", deleteException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, ScalarInt(connection, "SELECT COUNT(*) FROM dbo.Security_Audit_Events;"));
    }

    [Theory]
    [InlineData("changed_fields")]
    [InlineData("before_state_json")]
    [InlineData("after_state_json")]
    [InlineData("request_metadata")]
    public void AuditJsonColumns_RejectInvalidJson(string columnName)
    {
        using var connection = OpenKnownV0003Baseline();

        var exception = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection, $"""
            INSERT INTO dbo.Security_Audit_Events
                (event_code, entity_type, correlation_id, outcome, {columnName})
            VALUES ('TEST_BAD_JSON', 'SECURITY_TEST', NEWID(), 'FAILURE', N'not-json');
            """));
        Assert.Contains("Json", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuditSchema_HasRequiredFieldsNoSecretSpecificFieldsAndNoSqlView()
    {
        using var connection = OpenKnownV0003Baseline();
        var actualColumns = QueryStrings(connection, """
            SELECT name FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Security_Audit_Events')
            ORDER BY column_id;
            """);

        foreach (var required in new[]
        {
            "actor_user_id", "acting_as_user_id", "target_user_id", "company_id", "event_code",
            "entity_type", "entity_id", "changed_fields", "before_state_json", "after_state_json",
            "reason", "correlation_id", "request_metadata", "outcome", "policy_version", "created_at"
        })
        {
            Assert.Contains(required, actualColumns);
        }

        Assert.DoesNotContain(actualColumns, name =>
            name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("token", StringComparison.OrdinalIgnoreCase)
            || name.Contains("signing", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("file_bytes", StringComparison.OrdinalIgnoreCase)
            || name.Contains("signed_url", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, ScalarInt(connection, """
            SELECT COUNT(*) FROM sys.views WHERE name = N'vw_SECURITY_AUDIT_VIEW';
            """));
    }

    [Fact]
    public void Rollback_CleanSchemaSucceeds_ReturnsToV0002_AndV0003Reapplies()
    {
        using (var connection = OpenKnownV0003Baseline())
        {
            ExecuteRollback(connection);
            Assert.Equal(0, CountExpectedTables(connection));
            Assert.Equal(0, CountVersion(connection, "V0003"));
            Assert.Equal(1, CountVersion(connection, "V0002"));
        }

        var output = ExecuteDbMigrator();
        Assert.Contains("Applied V0003", output, StringComparison.Ordinal);
        using var reappliedConnection = _fixture.OpenVerifiedConnection();
        Assert.Equal(ExpectedTables.Length, CountExpectedTables(reappliedConnection));
        Assert.Equal(1, CountVersion(reappliedConnection, "V0003"));
    }

    [Fact]
    public void Rollback_RejectsMissingV0003()
    {
        _fixture.ResetToV0002();
        using var connection = _fixture.OpenVerifiedConnection();

        var exception = Assert.Throws<SqlException>(() => ExecuteRollback(connection));
        Assert.Contains("V0003 is not recorded", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, CountVersion(connection, "V0002"));
    }

    [Fact]
    public void Rollback_RejectsV0004OrLater_WithoutChangingSchemaVersions()
    {
        using var connection = OpenKnownV0003Baseline();
        ExecuteNonQuery(connection, """
            INSERT INTO dbo.SchemaVersions (Version, ScriptName, Status)
            VALUES ('V0004', 'V0004__future.sql', 'APPLIED');
            """);

        var exception = Assert.Throws<SqlException>(() => ExecuteRollback(connection));
        Assert.Contains("later than V0003", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, CountVersion(connection, "V0003"));
        Assert.Equal(1, CountVersion(connection, "V0004"));
        Assert.Equal(ExpectedTables.Length, CountExpectedTables(connection));
    }

    public static TheoryData<string> ProtectedRollbackCategories => new()
    {
        "User_Auth_Accounts",
        "Password_History",
        "Refresh_Tokens",
        "Role_Permissions",
        "Department_Permissions",
        "User_Role_Assignments",
        "User_Individual_Permissions",
        "Admin_Group_Permissions",
        "User_Admin_Group_Assignments",
        "Security_Audit_Events"
    };

    [Theory]
    [MemberData(nameof(ProtectedRollbackCategories))]
    public void Rollback_EachProtectedDataCategoryBlocksAndRemainsAtomic(string category)
    {
        using var connection = OpenKnownV0003Baseline();
        SeedProtectedCategory(connection, category);

        var exception = Assert.Throws<SqlException>(() => ExecuteRollback(connection));
        Assert.Contains(category, exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, CountVersion(connection, "V0003"));
        Assert.Equal(ExpectedTables.Length, CountExpectedTables(connection));
    }

    [Theory]
    [InlineData("Roles")]
    [InlineData("Admin_Groups")]
    public void Rollback_AnyRoleOrAdminGroupBlocks(string tableName)
    {
        using var connection = OpenKnownV0003Baseline();
        if (tableName == "Roles")
        {
            InsertRole(connection);
        }
        else
        {
            InsertAdminGroup(connection);
        }

        var exception = Assert.Throws<SqlException>(() => ExecuteRollback(connection));
        Assert.Contains(tableName, exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, CountVersion(connection, "V0003"));
    }

    [Fact]
    public void Rollback_ModifiedPermissionCatalogBlocks()
    {
        using var connection = OpenKnownV0003Baseline();
        ExecuteNonQuery(connection, """
            UPDATE dbo.Permissions
            SET description = N'Unauthorized catalog drift'
            WHERE permission_code = 'SECURITY_ROLE_VIEW';
            """);

        var exception = Assert.Throws<SqlException>(() => ExecuteRollback(connection));
        Assert.Contains("Permissions differs", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, CountVersion(connection, "V0003"));
    }

    [Fact]
    public void Rollback_NonPristinePolicyStateBlocks()
    {
        using var connection = OpenKnownV0003Baseline();
        ExecuteNonQuery(connection, """
            UPDATE dbo.Authorization_Policy_State
            SET policy_version = 2, updated_at = SYSUTCDATETIME();
            """);

        var exception = Assert.Throws<SqlException>(() => ExecuteRollback(connection));
        Assert.Contains("Authorization_Policy_State is not pristine", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, CountVersion(connection, "V0003"));
    }

    [Fact]
    public void Rollback_BootstrappedStateBlocks()
    {
        using var connection = OpenKnownV0003Baseline();
        var userId = InsertUser(connection);
        ExecuteNonQuery(connection, $"""
            UPDATE dbo.Security_Bootstrap_State
            SET is_bootstrapped = 1,
                bootstrapped_at = SYSUTCDATETIME(),
                bootstrapped_by_user_id = {userId};
            """);

        var exception = Assert.Throws<SqlException>(() => ExecuteRollback(connection));
        Assert.Contains("Security_Bootstrap_State is not pristine", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, CountVersion(connection, "V0003"));
    }

    [Fact]
    public void Rollback_FailureAfterDropsBegun_RollsBackEveryChange()
    {
        using var connection = OpenKnownV0003Baseline();
        CreateAuditRuntimeTestUser(connection);

        var exception = Assert.Throws<SqlException>(() => ExecuteRollback(connection));
        Assert.Contains("role", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, CountVersion(connection, "V0003"));
        Assert.Equal(ExpectedTables.Length, CountExpectedTables(connection));
        Assert.Equal(1, ScalarInt(connection, """
            SELECT COUNT(*) FROM sys.triggers
            WHERE name = N'TR_Security_Audit_Events_AppendOnly';
            """));
        Assert.Equal(1, ScalarInt(connection, """
            SELECT COUNT(*) FROM sys.database_principals
            WHERE name = N'PTKD_Security_Audit_Runtime' AND type = 'R';
            """));

        ExecuteNonQuery(connection, """
            ALTER ROLE PTKD_Security_Audit_Runtime DROP MEMBER PTKD_SecurityAuditRuntime_Test;
            DROP USER PTKD_SecurityAuditRuntime_Test;
            """);
    }

    private SqlConnection OpenKnownV0003Baseline()
    {
        _fixture.ResetToV0003();
        return _fixture.OpenVerifiedConnection();
    }

    private void ExecuteRollback(SqlConnection connection)
    {
        TestDatabaseFixture.ExecuteBatches(
            _fixture.ReadRollback("U0003__drop_security_schema.sql"),
            connection);
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

    private static int CountExpectedTables(SqlConnection connection)
    {
        var parameters = ExpectedTables
            .Select((name, index) => new SqlParameter($"@table{index}", name))
            .ToArray();
        var parameterNames = string.Join(", ", parameters.Select(parameter => parameter.ParameterName));
        return ScalarInt(
            connection,
            $"SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') AND name IN ({parameterNames});",
            parameters);
    }

    private static int CountVersion(SqlConnection connection, string version) =>
        ScalarInt(
            connection,
            "SELECT COUNT(*) FROM dbo.SchemaVersions WHERE Version = @version;",
            new SqlParameter("@version", version));

    private static int ScalarInt(
        SqlConnection connection,
        string sql,
        params SqlParameter[] parameters) =>
        Convert.ToInt32(Scalar(connection, sql, parameters));

    private static long ScalarLong(SqlConnection connection, string sql) =>
        Convert.ToInt64(Scalar(connection, sql));

    private static byte[] ScalarBytes(SqlConnection connection, string sql) =>
        (byte[])Scalar(connection, sql)!;

    private static object? Scalar(
        SqlConnection connection,
        string sql,
        params SqlParameter[] parameters)
    {
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return command.ExecuteScalar();
    }

    private static void ExecuteNonQuery(SqlConnection connection, string sql)
    {
        using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout = 60
        };
        command.ExecuteNonQuery();
    }

    private static string[] QueryStrings(SqlConnection connection, string sql)
    {
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        var values = new List<string>();
        while (reader.Read())
        {
            values.Add(reader.GetString(0));
        }

        return values.ToArray();
    }

    private static void AssertSqlConstraint(
        SqlConnection connection,
        string sql,
        string expectedConstraint)
    {
        var exception = Assert.Throws<SqlException>(() => ExecuteNonQuery(connection, sql));
        Assert.Contains(expectedConstraint, exception.Message, StringComparison.Ordinal);
    }

    private static long InsertUser(SqlConnection connection)
    {
        var employeeCode = "SEC_" + Guid.NewGuid().ToString("N")[..20];
        using var command = new SqlCommand("""
            INSERT INTO dbo.Users
                (employee_code, full_name, employment_status, account_status, created_at)
            OUTPUT inserted.id
            VALUES (@employeeCode, N'Security Test User', 'ACTIVE', 'ACTIVE', SYSUTCDATETIME());
            """, connection);
        command.Parameters.AddWithValue("@employeeCode", employeeCode);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static long InsertCompany(SqlConnection connection)
    {
        var companyCode = "SEC_" + Guid.NewGuid().ToString("N")[..20];
        using var command = new SqlCommand("""
            INSERT INTO dbo.Companies (company_code, name, is_active, created_at)
            OUTPUT inserted.id
            VALUES (@companyCode, N'Security Test Company', 1, SYSUTCDATETIME());
            """, connection);
        command.Parameters.AddWithValue("@companyCode", companyCode);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static long InsertDepartment(SqlConnection connection, long companyId)
    {
        var departmentCode = "SEC_" + Guid.NewGuid().ToString("N")[..20];
        using var command = new SqlCommand("""
            INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at)
            OUTPUT inserted.id
            VALUES (@departmentCode, @companyId, N'Security Test Department', 1, SYSUTCDATETIME());
            """, connection);
        command.Parameters.AddWithValue("@departmentCode", departmentCode);
        command.Parameters.AddWithValue("@companyId", companyId);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static long InsertRole(SqlConnection connection)
    {
        var roleCode = "ROLE_" + Guid.NewGuid().ToString("N")[..20];
        using var command = new SqlCommand("""
            INSERT INTO dbo.Roles (role_code, name, scope_type, company_id)
            OUTPUT inserted.id
            VALUES (@roleCode, N'Security Test Role', 'GLOBAL', NULL);
            """, connection);
        command.Parameters.AddWithValue("@roleCode", roleCode);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static long InsertAdminGroup(SqlConnection connection)
    {
        var groupCode = "GROUP_" + Guid.NewGuid().ToString("N")[..20];
        using var command = new SqlCommand("""
            INSERT INTO dbo.Admin_Groups (group_code, name, scope_type, company_id)
            OUTPUT inserted.id
            VALUES (@groupCode, N'Security Test Admin Group', 'GLOBAL', NULL);
            """, connection);
        command.Parameters.AddWithValue("@groupCode", groupCode);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static long InsertAccount(SqlConnection connection, long userId)
    {
        using var command = new SqlCommand("""
            INSERT INTO dbo.User_Auth_Accounts
                (user_id, provider_type, provider_subject, password_hash)
            OUTPUT inserted.id
            VALUES (@userId, 'INTERNAL', @subject, 'test-password-hash');
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@subject", "subject-" + Guid.NewGuid().ToString("N"));
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static void SeedProtectedCategory(SqlConnection connection, string category)
    {
        switch (category)
        {
            case "User_Auth_Accounts":
                InsertAccount(connection, InsertUser(connection));
                break;
            case "Password_History":
            {
                var accountId = InsertAccount(connection, InsertUser(connection));
                ExecuteNonQuery(connection,
                    $"INSERT INTO dbo.Password_History (account_id, password_hash) VALUES ({accountId}, 'history-hash');");
                break;
            }
            case "Refresh_Tokens":
            {
                var accountId = InsertAccount(connection, InsertUser(connection));
                ExecuteNonQuery(connection, $"""
                    INSERT INTO dbo.Refresh_Tokens
                        (account_id, token_hash, family_id, session_id, expires_at)
                    VALUES ({accountId}, REPLICATE('B', 64), NEWID(), NEWID(), DATEADD(day, 1, SYSUTCDATETIME()));
                    """);
                break;
            }
            case "Role_Permissions":
            {
                var roleId = InsertRole(connection);
                ExecuteNonQuery(connection,
                    $"INSERT INTO dbo.Role_Permissions (role_id, permission_code) VALUES ({roleId}, 'SECURITY_ROLE_VIEW');");
                break;
            }
            case "Department_Permissions":
            {
                var companyId = InsertCompany(connection);
                var departmentId = InsertDepartment(connection, companyId);
                ExecuteNonQuery(connection,
                    $"INSERT INTO dbo.Department_Permissions (department_id, permission_code) VALUES ({departmentId}, 'SECURITY_ROLE_VIEW');");
                break;
            }
            case "User_Role_Assignments":
            {
                var userId = InsertUser(connection);
                var roleId = InsertRole(connection);
                ExecuteNonQuery(connection,
                    $"INSERT INTO dbo.User_Role_Assignments (user_id, role_id) VALUES ({userId}, {roleId});");
                break;
            }
            case "User_Individual_Permissions":
            {
                var userId = InsertUser(connection);
                ExecuteNonQuery(connection, $"""
                    INSERT INTO dbo.User_Individual_Permissions
                        (user_id, permission_code, scope_type, company_id, grant_type)
                    VALUES ({userId}, 'SECURITY_ROLE_VIEW', 'GLOBAL', NULL, 'ALLOW');
                    """);
                break;
            }
            case "Admin_Group_Permissions":
            {
                var groupId = InsertAdminGroup(connection);
                ExecuteNonQuery(connection,
                    $"INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code) VALUES ({groupId}, 'SECURITY_ROLE_VIEW');");
                break;
            }
            case "User_Admin_Group_Assignments":
            {
                var userId = InsertUser(connection);
                var groupId = InsertAdminGroup(connection);
                ExecuteNonQuery(connection,
                    $"INSERT INTO dbo.User_Admin_Group_Assignments (user_id, admin_group_id) VALUES ({userId}, {groupId});");
                break;
            }
            case "Security_Audit_Events":
                ExecuteNonQuery(connection, """
                    INSERT INTO dbo.Security_Audit_Events
                        (event_code, entity_type, correlation_id, outcome)
                    VALUES ('ROLLBACK_PROTECTION_TEST', 'SECURITY_TEST', NEWID(), 'SUCCESS');
                    """);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown protected category.");
        }
    }

    private static void CreateAuditRuntimeTestUser(SqlConnection connection)
    {
        ExecuteNonQuery(connection, """
            CREATE USER PTKD_SecurityAuditRuntime_Test WITHOUT LOGIN;
            ALTER ROLE PTKD_Security_Audit_Runtime ADD MEMBER PTKD_SecurityAuditRuntime_Test;
            """);
    }

    private static void ExecuteAsAuditRuntimeUser(SqlConnection connection, string sql)
    {
        ExecuteNonQuery(connection, "EXECUTE AS USER = 'PTKD_SecurityAuditRuntime_Test';");
        try
        {
            ExecuteNonQuery(connection, sql);
        }
        finally
        {
            ExecuteNonQuery(connection, "REVERT;");
        }
    }

    private static object? ExecuteScalarAsAuditRuntimeUser(SqlConnection connection, string sql)
    {
        ExecuteNonQuery(connection, "EXECUTE AS USER = 'PTKD_SecurityAuditRuntime_Test';");
        try
        {
            return Scalar(connection, sql);
        }
        finally
        {
            ExecuteNonQuery(connection, "REVERT;");
        }
    }

    private static int[] ExecutePermissionProbeAsAuditRuntimeUser(SqlConnection connection)
    {
        ExecuteNonQuery(connection, "EXECUTE AS USER = 'PTKD_SecurityAuditRuntime_Test';");
        try
        {
            using var command = new SqlCommand("""
                SELECT
                    HAS_PERMS_BY_NAME('dbo.Security_Audit_Events', 'OBJECT', 'SELECT'),
                    HAS_PERMS_BY_NAME('dbo.Security_Audit_Events', 'OBJECT', 'INSERT'),
                    HAS_PERMS_BY_NAME('dbo.Security_Audit_Events', 'OBJECT', 'UPDATE'),
                    HAS_PERMS_BY_NAME('dbo.Security_Audit_Events', 'OBJECT', 'DELETE'),
                    HAS_PERMS_BY_NAME('dbo.Security_Audit_Events', 'OBJECT', 'ALTER');
                """, connection);
            using var reader = command.ExecuteReader();
            Assert.True(reader.Read());
            return Enumerable.Range(0, 5).Select(reader.GetInt32).ToArray();
        }
        finally
        {
            ExecuteNonQuery(connection, "REVERT;");
        }
    }

    public void Dispose()
    {
        _fixture.ResetToV0002();
    }
}
