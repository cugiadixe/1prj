SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- U0011: Rollback Service Module Foundation
-- Phase 1B.6-B Service Module Foundation
-- ============================================================

-- 1. Drop Service_History (depends on Services)
IF OBJECT_ID('dbo.Service_History', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Service_History;
END

-- 2. Drop Services (depends on Service_Types, Customers, Companies)
IF OBJECT_ID('dbo.Services', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Services;
END

-- 3. Drop Service_Price_History (depends on Service_Types)
IF OBJECT_ID('dbo.Service_Price_History', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Service_Price_History;
END

-- 4. Drop Service_Types
IF OBJECT_ID('dbo.Service_Types', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Service_Types;
END

-- 5. Soft-deactivate permissions (TR_Permissions_PreventDelete)
UPDATE dbo.Permissions
SET is_active = 0
WHERE permission_code IN (
    'SERVICE_VIEW',
    'SERVICE_TYPE_MANAGE',
    'SERVICE_CREATE_STANDARD',
    'SERVICE_RENEW_STANDARD',
    'SERVICE_PRICE_OVERRIDE_REQUEST',
    'SERVICE_PRICE_OVERRIDE_APPROVE'
);

-- 6. Soft-deactivate business process catalog entries
UPDATE dbo.Business_Process_Catalog
SET is_active = 0, updated_at = SYSUTCDATETIME()
WHERE process_code IN ('SERVICE_PRICE_OVERRIDE', 'RENEW_SERVICE_STANDARD');

-- 7. Remove V0011 from SchemaVersions
DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0011%';

COMMIT TRANSACTION;
