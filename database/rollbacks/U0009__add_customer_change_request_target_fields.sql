SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;

-- ============================================================
-- U0009: Rollback target fields for CUSTOMER_MASTER_CHANGE
-- Reverse of V0009__add_customer_change_request_target_fields.sql
-- ============================================================

-- Guard: only run against test database
IF DB_NAME() <> N'PTKD_TEST_PHASE1A2'
BEGIN
    RAISERROR('U0009 rollback is only allowed on PTKD_TEST_PHASE1A2.', 16, 1);
END;

-- Guard: SchemaVersions must exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SchemaVersions')
BEGIN
    RAISERROR('SchemaVersions table does not exist.', 16, 1);
END;

-- Guard: V0009 must be recorded
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0009%')
BEGIN
    RAISERROR('V0009 migration is not recorded in SchemaVersions.', 16, 1);
END;

ALTER TABLE dbo.[Customer_Change_Requests] DROP CONSTRAINT IF EXISTS [FK_Customer_Change_Requests_TargetCustomer];

DROP INDEX IF EXISTS [IX_CCR_target_customer] ON dbo.[Customer_Change_Requests];

-- Wait, what if the columns are dropped?
IF EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'target_customer_id' AND Object_ID = Object_ID(N'dbo.Customer_Change_Requests'))
BEGIN
    ALTER TABLE dbo.[Customer_Change_Requests] DROP COLUMN [target_customer_id];
END;

IF EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'target_row_version' AND Object_ID = Object_ID(N'dbo.Customer_Change_Requests'))
BEGIN
    ALTER TABLE dbo.[Customer_Change_Requests] DROP COLUMN [target_row_version];
END;

-- Remove V0009 from SchemaVersions
DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0009%';

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
GO
