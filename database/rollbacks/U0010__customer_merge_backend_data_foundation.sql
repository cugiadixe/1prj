SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- U0010: Rollback Customer Merge Backend/Data Foundation
-- Phase 1B.5-B Customer Merge
-- ============================================================

-- Reverse Seed Permissions
UPDATE dbo.Permissions
SET is_active = 0
WHERE permission_code IN (
    'CUSTOMER_MERGE_REQUEST_CREATE',
    'CUSTOMER_MERGE_REQUEST_VIEW',
    'CUSTOMER_MERGE_REQUEST_ADMIN_VIEW',
    'CUSTOMER_MERGE_EXECUTE'
);

-- Drop Audit/History
IF OBJECT_ID('dbo.Customer_Merge_History', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Customer_Merge_History;
END

-- Drop Candidates
IF OBJECT_ID('dbo.Customer_Merge_Request_Candidates', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Customer_Merge_Request_Candidates;
END

-- Drop Requests
IF OBJECT_ID('dbo.Customer_Merge_Requests', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Customer_Merge_Requests;
END

-- Remove V0010 from SchemaVersions
DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0010%';

COMMIT TRANSACTION;
