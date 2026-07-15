-- U0003__drop_security_schema.sql
-- Guarded rollback for V0003.

-- 1. Guard against rolling back if V0004 or later is applied.
IF EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version > 'V0003' AND Status = 'APPLIED')
BEGIN
    RAISERROR('Cannot rollback V0003 because a later migration has been applied.', 16, 1);
    RETURN;
END

-- 2. Guard against DB name (only PTKD_TEST_PHASE1A2)
IF DB_NAME() NOT LIKE 'PTKD_TEST_%'
BEGIN
    RAISERROR('Rollback U0003 can only be executed automatically in test databases.', 16, 1);
    RETURN;
END

-- 3. Data protection guard: Do not rollback if material security data exists.
DECLARE @DataCount INT;
SELECT @DataCount = COUNT(*) FROM dbo.User_Auth_Accounts;
IF @DataCount > 0
BEGIN
    RAISERROR('Cannot automatically drop security schema: User_Auth_Accounts contains populated data. Manual recovery required.', 16, 1);
    RETURN;
END

-- Proceed with safe dropping in reverse dependency order
DROP VIEW IF EXISTS dbo.vw_SECURITY_AUDIT_VIEW;

DROP TABLE IF EXISTS dbo.Security_Bootstrap_State;
DROP TABLE IF EXISTS dbo.Authorization_Policy_State;

DROP TABLE IF EXISTS dbo.User_Admin_Group_Assignments;
DROP TABLE IF EXISTS dbo.User_Individual_Permissions;
DROP TABLE IF EXISTS dbo.User_Role_Assignments;
DROP TABLE IF EXISTS dbo.Admin_Group_Permissions;
DROP TABLE IF EXISTS dbo.Admin_Groups;
DROP TABLE IF EXISTS dbo.Department_Permissions;
DROP TABLE IF EXISTS dbo.Role_Permissions;
DROP TABLE IF EXISTS dbo.Roles;
DROP TABLE IF EXISTS dbo.Permissions;
DROP TABLE IF EXISTS dbo.Refresh_Tokens;
DROP TABLE IF EXISTS dbo.Password_History;
DROP TABLE IF EXISTS dbo.User_Auth_Accounts;

-- Drop trigger then table
IF OBJECT_ID('dbo.TR_Security_Audit_Events_PreventUpdateDelete', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_Security_Audit_Events_PreventUpdateDelete;

DROP TABLE IF EXISTS dbo.Security_Audit_Events;

-- Remove SchemaVersion if present
DELETE FROM dbo.SchemaVersions WHERE Version = 'V0003';
GO
