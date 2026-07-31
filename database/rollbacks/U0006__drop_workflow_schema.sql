SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;

-- ============================================================
-- U0006: Drop Workflow/Approval Engine Schema
-- Reverse of V0006__create_workflow_schema.sql
-- ============================================================

-- Guard: only run against test database
IF DB_NAME() <> N'PTKD_TEST_PHASE1A2'
BEGIN
    RAISERROR('U0006 rollback is only allowed on PTKD_TEST_PHASE1A2.', 16, 1);
END;

-- Guard: SchemaVersions must exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SchemaVersions')
BEGIN
    RAISERROR('SchemaVersions table does not exist.', 16, 1);
END;

-- Guard: V0006 must be recorded
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0006%')
BEGIN
    RAISERROR('V0006 migration is not recorded in SchemaVersions.', 16, 1);
END;

-- Guard: no later migration
IF EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0007%')
BEGIN
    RAISERROR('A later migration (V0007+) exists. Roll that back first.', 16, 1);
END;

-- Drop tables in reverse dependency order
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

-- Deactivate workflow permissions (not DELETE due to audit trigger)
UPDATE dbo.Permissions
SET is_active = 0
WHERE permission_code IN (
    'WORKFLOW_VIEW',
    'WORKFLOW_CONFIG_MANAGE',
    'WORKFLOW_PUBLISH',
    'WORKFLOW_BIND_PROCESS',
    'WORKFLOW_REASSIGN_PENDING',
    'WORKFLOW_AUDIT_VIEW'
);

-- Remove V0006 from SchemaVersions
DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0006%';

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
