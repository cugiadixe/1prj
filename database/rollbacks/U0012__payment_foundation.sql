SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- U0012: Rollback Payment Foundation
-- Phase 1B.7-B Payment Backend/Data Foundation
-- ============================================================

-- 1. Drop Payment_Correction_History (depends on Payment_Transactions)
IF OBJECT_ID('dbo.Payment_Correction_History', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Payment_Correction_History;
END

-- 2. Drop Payment_Transaction_Items (depends on Payment_Transactions, Services)
IF OBJECT_ID('dbo.Payment_Transaction_Items', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Payment_Transaction_Items;
END

-- 3. Drop Reconciliation_Periods (depends on Companies)
IF OBJECT_ID('dbo.Reconciliation_Periods', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Reconciliation_Periods;
END

-- 4. Drop Payment_Transactions (depends on Customers, Companies, Users)
IF OBJECT_ID('dbo.Payment_Transactions', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Payment_Transactions;
END

-- 5. Soft-deactivate permissions (TR_Permissions_PreventDelete)
UPDATE dbo.Permissions
SET is_active = 0
WHERE permission_code IN (
    'PAYMENT_CREATE_DRAFT',
    'PAYMENT_CONFIRM',
    'PAYMENT_PRINT',
    'PAYMENT_CORRECT_CONFIRMED',
    'RECONCILIATION_PREPARE',
    'RECONCILIATION_CONFIRM'
);

-- 6. Remove V0012 from SchemaVersions
DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0012%';

COMMIT TRANSACTION;
