-- U0003__drop_security_schema.sql
-- Test-only, data-protecting rollback for V0003.

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF DB_NAME() <> N'PTKD_TEST_PHASE1A2'
        THROW 51100, 'U0003 may run only against the exact database PTKD_TEST_PHASE1A2.', 1;

    IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
        THROW 51101, 'SchemaVersions is missing; V0003 cannot be rolled back.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SchemaVersions
        WHERE Version = N'V0003'
          AND ScriptName = N'V0003__create_security_schema.sql'
    )
        THROW 51102, 'V0003 is not recorded in SchemaVersions.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SchemaVersions
        WHERE Version LIKE N'V%'
          AND TRY_CONVERT(int, SUBSTRING(Version, 2, 20)) > 3
    )
        THROW 51103, 'A migration later than V0003 is recorded; rollback is prohibited.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Password_History)
        THROW 51111, 'Rollback blocked: Password_History contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Refresh_Tokens)
        THROW 51112, 'Rollback blocked: Refresh_Tokens contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.User_Auth_Accounts)
        THROW 51110, 'Rollback blocked: User_Auth_Accounts contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Role_Permissions)
        THROW 51113, 'Rollback blocked: Role_Permissions contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Department_Permissions)
        THROW 51114, 'Rollback blocked: Department_Permissions contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.User_Role_Assignments)
        THROW 51115, 'Rollback blocked: User_Role_Assignments contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions)
        THROW 51116, 'Rollback blocked: User_Individual_Permissions contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions)
        THROW 51117, 'Rollback blocked: Admin_Group_Permissions contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.User_Admin_Group_Assignments)
        THROW 51118, 'Rollback blocked: User_Admin_Group_Assignments contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Security_Audit_Events)
        THROW 51119, 'Rollback blocked: Security_Audit_Events contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Roles)
        THROW 51120, 'Rollback blocked: Roles contains material data.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Admin_Groups)
        THROW 51121, 'Rollback blocked: Admin_Groups contains material data.', 1;

    IF (SELECT COUNT(*) FROM dbo.Permissions) <> 15
        THROW 51122, 'Rollback blocked: Permissions differs from the approved seed catalog.', 1;

    IF EXISTS (
        SELECT permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description
        FROM dbo.Permissions
        EXCEPT
        SELECT *
        FROM (VALUES
            (CAST('ORGANIZATION_COMPANY_VIEW'      AS varchar(100)), CAST('ORGANIZATION' AS varchar(50)), CAST('COMPANY_VIEW'       AS varchar(50)), CAST('GLOBAL'  AS varchar(30)), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(N'View companies.' AS nvarchar(500))),
            ('ORGANIZATION_COMPANY_MANAGE',     'ORGANIZATION', 'COMPANY_MANAGE',     'GLOBAL',  1, 1, 0, 1, N'Manage companies.'),
            ('ORGANIZATION_DEPARTMENT_VIEW',    'ORGANIZATION', 'DEPARTMENT_VIEW',    'GLOBAL',  0, 0, 0, 1, N'View departments.'),
            ('ORGANIZATION_DEPARTMENT_MANAGE',  'ORGANIZATION', 'DEPARTMENT_MANAGE',  'GLOBAL',  1, 1, 0, 1, N'Manage departments.'),
            ('SECURITY_USER_VIEW',              'SECURITY',     'USER_VIEW',          'GLOBAL',  1, 0, 0, 1, N'View security user administration data.'),
            ('SECURITY_USER_MANAGE',            'SECURITY',     'USER_MANAGE',        'GLOBAL',  1, 1, 0, 1, N'Manage security user administration data.'),
            ('SECURITY_ASSIGNMENT_MANAGE',      'SECURITY',     'ASSIGNMENT_MANAGE',  'COMPANY', 1, 1, 0, 1, N'Manage scoped security assignments.'),
            ('SECURITY_ROLE_VIEW',              'SECURITY',     'ROLE_VIEW',          'GLOBAL',  1, 0, 0, 1, N'View security roles.'),
            ('SECURITY_ROLE_MANAGE',            'SECURITY',     'ROLE_MANAGE',        'GLOBAL',  1, 1, 0, 1, N'Manage security roles.'),
            ('SECURITY_PERMISSION_VIEW',        'SECURITY',     'PERMISSION_VIEW',    'GLOBAL',  1, 0, 0, 1, N'View the security permission catalog.'),
            ('SECURITY_PERMISSION_MANAGE',      'SECURITY',     'PERMISSION_MANAGE',  'GLOBAL',  1, 1, 0, 1, N'Manage the security permission catalog.'),
            ('SECURITY_ACCOUNT_MANAGE',         'SECURITY',     'ACCOUNT_MANAGE',     'GLOBAL',  1, 1, 0, 1, N'Manage authentication accounts and sessions.'),
            ('SECURITY_ADMIN_GROUP_VIEW',       'SECURITY',     'ADMIN_GROUP_VIEW',   'GLOBAL',  1, 0, 0, 1, N'View security Admin Groups.'),
            ('SECURITY_ADMIN_GROUP_MANAGE',     'SECURITY',     'ADMIN_GROUP_MANAGE', 'GLOBAL',  1, 1, 0, 1, N'Manage security Admin Groups.'),
            ('SECURITY_AUDIT_VIEW',             'SECURITY',     'AUDIT_VIEW',         'GLOBAL',  1, 1, 0, 1, N'View authentication, authorization, and security administration audit events.')
        ) AS approved(permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    )
        THROW 51122, 'Rollback blocked: Permissions differs from the approved seed catalog.', 1;

    IF (SELECT COUNT(*) FROM dbo.Authorization_Policy_State) <> 1
       OR NOT EXISTS (
            SELECT 1
            FROM dbo.Authorization_Policy_State
            WHERE id = 1
              AND policy_version = 1
              AND updated_at = CONVERT(datetime2(3), '1900-01-01T00:00:00.000')
              AND updated_by_user_id IS NULL
       )
        THROW 51123, 'Rollback blocked: Authorization_Policy_State is not pristine.', 1;

    IF (SELECT COUNT(*) FROM dbo.Security_Bootstrap_State) <> 1
       OR NOT EXISTS (
            SELECT 1
            FROM dbo.Security_Bootstrap_State
            WHERE id = 1
              AND is_bootstrapped = 0
              AND bootstrapped_at IS NULL
              AND bootstrapped_by_user_id IS NULL
       )
        THROW 51124, 'Rollback blocked: Security_Bootstrap_State is not pristine.', 1;

    DROP TRIGGER dbo.TR_User_Admin_Group_Assignments_PreventOverlap;
    DROP TRIGGER dbo.TR_User_Individual_Permissions_PreventOverlap;
    DROP TRIGGER dbo.TR_User_Role_Assignments_PreventOverlap;
    DROP TRIGGER dbo.TR_Password_History_AppendOnly;
    DROP TRIGGER dbo.TR_Permissions_PreventCodeChange;
    DROP TRIGGER dbo.TR_Permissions_PreventDelete;
    DROP TRIGGER dbo.TR_Security_Audit_Events_AppendOnly;

    DROP ROLE PTKD_Security_Audit_Runtime;

    DROP TABLE dbo.User_Admin_Group_Assignments;
    DROP TABLE dbo.Admin_Group_Permissions;
    DROP TABLE dbo.Admin_Groups;
    DROP TABLE dbo.User_Individual_Permissions;
    DROP TABLE dbo.User_Role_Assignments;
    DROP TABLE dbo.Department_Permissions;
    DROP TABLE dbo.Role_Permissions;
    DROP TABLE dbo.Roles;
    DROP TABLE dbo.Refresh_Tokens;
    DROP TABLE dbo.Password_History;
    DROP TABLE dbo.User_Auth_Accounts;
    DROP TABLE dbo.Security_Audit_Events;
    DROP TABLE dbo.Security_Bootstrap_State;
    DROP TABLE dbo.Authorization_Policy_State;
    DROP TABLE dbo.Permissions;

    DELETE FROM dbo.SchemaVersions
    WHERE Version = N'V0003'
      AND ScriptName = N'V0003__create_security_schema.sql';

    IF @@ROWCOUNT <> 1
        THROW 51125, 'V0003 SchemaVersions row was not removed exactly once.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
