SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;

-- ============================================================
-- U0008: Revert harden workflow runtime
-- Reverse of V0008__harden_workflow_runtime.sql
-- ============================================================

-- Guard: only run against test database
IF DB_NAME() <> N'PTKD_TEST_PHASE1A2'
BEGIN
    RAISERROR('U0008 rollback is only allowed on PTKD_TEST_PHASE1A2.', 16, 1);
END;

-- Guard: SchemaVersions must exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SchemaVersions')
BEGIN
    RAISERROR('SchemaVersions table does not exist.', 16, 1);
END;

-- Guard: V0008 must be recorded
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0008%')
BEGIN
    RAISERROR('V0008 migration is not recorded in SchemaVersions.', 16, 1);
END;

ALTER TABLE dbo.Workflow_Actions DROP CONSTRAINT CK_WA_action_type;
ALTER TABLE dbo.Workflow_Actions ADD CONSTRAINT CK_WA_action_type CHECK (action_type IN ('APPROVE', 'RETURN', 'REASSIGN'));

ALTER TABLE dbo.Workflow_Instances DROP CONSTRAINT CK_WI_instance_status;
ALTER TABLE dbo.Workflow_Instances ADD CONSTRAINT CK_WI_instance_status CHECK (instance_status IN ('PENDING_APPROVAL', 'APPROVED', 'RETURNED', 'WITHDRAWN', 'PENDING_EXECUTION', 'EXECUTING', 'EXECUTED', 'FAILED'));

-- Remove V0008 from SchemaVersions
DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0008%';

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
