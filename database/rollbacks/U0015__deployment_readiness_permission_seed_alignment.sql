SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- U0015: Rollback Deployment Readiness Permission Seed Alignment
-- Phase 1B.10-B Deployment Readiness Remediation
-- Soft-deactivates permissions and process catalog entry
-- introduced by V0015.
-- ============================================================

-- 1. Soft-deactivate permissions introduced by V0015
-- Uses standard soft-deactivation pattern (respects TR_Permissions_PreventDelete).
UPDATE dbo.Permissions
SET is_active = 0
WHERE permission_code IN (
    'CARE_PACKAGE_APPROVE',
    'CARE_PACKAGE_REJECT',
    'CARE_PACKAGE_CREATE_PAYMENT',
    'CARD_REPRINT_REQUEST_CREATE',
    'CARD_REPRINT_REQUEST_VIEW',
    'CARD_REPRINT_APPROVE',
    'CARD_REPRINT_REQUEST_REJECT',
    'CARD_REPRINT_REQUEST_MARK_PRINTED',
    'WORKFLOW_REJECT',
    'WORKFLOW_RETRY_EXECUTION',
    'ORGANIZATION_USER_MANAGE',
    'CUSTOMER_CHANGE_REQUEST_CREATE'
);

-- 2. Soft-deactivate SELL_CARE_PACKAGE business process catalog entry
UPDATE dbo.Business_Process_Catalog
SET is_active = 0, updated_at = SYSUTCDATETIME()
WHERE process_code = 'SELL_CARE_PACKAGE';

-- 3. Remove V0015 from SchemaVersions
DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0015%';

COMMIT TRANSACTION;
