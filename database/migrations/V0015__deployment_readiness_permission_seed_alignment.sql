SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- V0015: Deployment Readiness Permission Seed Alignment
-- Phase 1B.10-B Deployment Readiness Remediation
-- Seeds 12 missing permission rows and SELL_CARE_PACKAGE
-- business process catalog entry.
-- ============================================================

-- 1. Care Package permissions (Phase 1B.9 gaps)
-- CARE_PACKAGE_VIEW and CARE_PACKAGE_CREATE already seeded in V0014 (module_code = 'SALES').
-- Remaining 3 codes follow the same module_code for consistency.

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CARE_PACKAGE_APPROVE')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CARE_PACKAGE_APPROVE', 'SALES', 'APPROVE', 'COMPANY', 0, 0, 0, 1, N'Approve a care package request requiring approval.');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CARE_PACKAGE_REJECT')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CARE_PACKAGE_REJECT', 'SALES', 'REJECT', 'COMPANY', 0, 0, 0, 1, N'Reject a care package request requiring approval.');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CARE_PACKAGE_CREATE_PAYMENT')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CARE_PACKAGE_CREATE_PAYMENT', 'SALES', 'CREATE_PAYMENT', 'COMPANY', 0, 0, 0, 1, N'Create payment for a payment-eligible care package request.');
END

-- 2. Card Reprint permissions (Phase 1B.8 gaps)

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CARD_REPRINT_REQUEST_CREATE')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CARD_REPRINT_REQUEST_CREATE', 'CARD', 'REQUEST_CREATE', 'COMPANY', 0, 0, 0, 1, N'Create a card reprint request.');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CARD_REPRINT_REQUEST_VIEW')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CARD_REPRINT_REQUEST_VIEW', 'CARD', 'REQUEST_VIEW', 'COMPANY', 0, 0, 0, 1, N'View card reprint requests within assigned company.');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CARD_REPRINT_APPROVE')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CARD_REPRINT_APPROVE', 'CARD', 'APPROVE_REPRINT', 'COMPANY', 1, 0, 1, 1, N'Approve card reprint.');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CARD_REPRINT_REQUEST_REJECT')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CARD_REPRINT_REQUEST_REJECT', 'CARD', 'REQUEST_REJECT', 'COMPANY', 0, 0, 0, 1, N'Reject a card reprint request.');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CARD_REPRINT_REQUEST_MARK_PRINTED')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CARD_REPRINT_REQUEST_MARK_PRINTED', 'CARD', 'REQUEST_MARK_PRINTED', 'COMPANY', 0, 0, 0, 1, N'Mark a card reprint request as printed.');
END

-- 3. Workflow permissions (Phase 1B.3 gaps)

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'WORKFLOW_REJECT')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('WORKFLOW_REJECT', 'WORKFLOW', 'REJECT', 'COMPANY', 1, 0, 0, 1, N'Reject a pending workflow request, terminating it.');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'WORKFLOW_RETRY_EXECUTION')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('WORKFLOW_RETRY_EXECUTION', 'WORKFLOW', 'RETRY_EXECUTION', 'GLOBAL', 1, 0, 0, 1, N'Retry a failed workflow execution.');
END

-- 4. Organization permission (Phase 1B.1 gap)

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'ORGANIZATION_USER_MANAGE')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('ORGANIZATION_USER_MANAGE', 'ORGANIZATION', 'USER_MANAGE', 'GLOBAL', 0, 0, 0, 1, N'Manage Organization Users API access in Phase 1B.');
END

-- 5. Customer permission (Phase 1B.4 gap)

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CUSTOMER_CHANGE_REQUEST_CREATE')
BEGIN
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CUSTOMER_CHANGE_REQUEST_CREATE', 'CUSTOMER', 'PROPOSE_CHANGE', 'GLOBAL', 0, 0, 0, 1, N'Submit customer create/change proposals.');
END

-- 6. Seed SELL_CARE_PACKAGE business process catalog entry

IF NOT EXISTS (SELECT 1 FROM dbo.Business_Process_Catalog WHERE process_code = 'SELL_CARE_PACKAGE')
BEGIN
    INSERT INTO dbo.Business_Process_Catalog (process_code, process_name, description, is_approval_required, is_active, created_at)
    VALUES ('SELL_CARE_PACKAGE', N'Bán gói chăm sóc', N'Quy trình bán gói chăm sóc, phê duyệt khi có chiết khấu hoặc giá ngoại lệ', 1, 1, SYSUTCDATETIME());
END

COMMIT TRANSACTION;
