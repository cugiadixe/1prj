SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;

-- ============================================================
-- U0007: Drop Customer_Change_Requests table
-- Reverse of V0007__create_customer_change_request.sql
-- ============================================================

-- Guard: only run against test database
IF DB_NAME() <> N'PTKD_TEST_PHASE1A2'
BEGIN
    RAISERROR('U0007 rollback is only allowed on PTKD_TEST_PHASE1A2.', 16, 1);
END;

-- Guard: SchemaVersions must exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SchemaVersions')
BEGIN
    RAISERROR('SchemaVersions table does not exist.', 16, 1);
END;

-- Guard: V0007 must be recorded
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0007%')
BEGIN
    RAISERROR('V0007 migration is not recorded in SchemaVersions.', 16, 1);
END;

-- Drop table
DROP TABLE IF EXISTS dbo.Customer_Change_Requests;

-- Remove V0007 from SchemaVersions
DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0007%';

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
