-- V0031__seed_assign_care_package_workflow.sql
-- Seed quy trình phê duyệt mẫu cho ASSIGN_CARE_PACKAGE (Nhóm E), để pilot bật được ngay:
--   Định nghĩa "Gán gói dịch vụ cho khách" → 1 bước "Trưởng phòng duyệt"
--   (nguồn người duyệt APPROVAL_AUTHORITY, cấp 1) → binding GLOBAL đang hiệu lực.
--
-- Trước tiên NỚI ràng buộc CK_WSAR_source_type để chấp nhận APPROVAL_AUTHORITY
-- (V0006 chưa có loại này).
--
-- Ghi chú hành vi: binding GLOBAL nên mọi công ty gửi X-Company-Id khi gán gói sẽ đi qua
-- quy trình. Phòng nào CHƯA khai báo thẩm quyền (bảng Approval_Authorities) → resolver trả
-- rỗng → service tự động duyệt CÓ ghi dấu (không chặn). Phòng có khai báo → chờ trưởng phòng duyệt.
--
-- Idempotent: chỉ seed nếu chưa có definition_code = 'ASSIGN_CARE_PACKAGE_DEFAULT'.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 1. Nới ràng buộc loại nguồn người duyệt: thêm APPROVAL_AUTHORITY
-- ══════════════════════════════════════════════════════════════════════════

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_WSAR_source_type')
    ALTER TABLE dbo.Workflow_Step_Approver_Rules DROP CONSTRAINT CK_WSAR_source_type;
GO

ALTER TABLE dbo.Workflow_Step_Approver_Rules
    ADD CONSTRAINT CK_WSAR_source_type CHECK (approver_source_type IN (
        'SPECIFIC_USER', 'ROLE', 'DEPARTMENT', 'DEPARTMENT_MANAGER',
        'REQUESTER_MANAGER', 'PERMISSION', 'ADMIN_GROUP', 'APPROVAL_AUTHORITY'));
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 2. Seed định nghĩa + phiên bản ACTIVE + bước + quy tắc người duyệt + binding
-- ══════════════════════════════════════════════════════════════════════════

DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Workflow_Definitions WHERE definition_code = 'ASSIGN_CARE_PACKAGE_DEFAULT')
BEGIN
    DECLARE @now datetime2(3) = SYSUTCDATETIME();
    DECLARE @defId BIGINT, @verId BIGINT, @stepId BIGINT;

    INSERT INTO dbo.Workflow_Definitions
        (definition_code, definition_name, description, process_code, is_active, created_by, created_at)
    VALUES
        ('ASSIGN_CARE_PACKAGE_DEFAULT', N'Gán gói dịch vụ cho khách hàng',
         N'Quy trình mẫu (pilot): nhân viên tạo, trưởng phòng duyệt rồi mới gán được vào mộ.',
         'ASSIGN_CARE_PACKAGE', 1, @adminId, @now);
    SET @defId = SCOPE_IDENTITY();

    INSERT INTO dbo.Workflow_Definition_Versions
        (workflow_definition_id, version_number, version_status, effective_from, published_at, published_by, created_by, created_at)
    VALUES
        (@defId, 1, 'ACTIVE', @now, @now, @adminId, @adminId, @now);
    SET @verId = SCOPE_IDENTITY();

    INSERT INTO dbo.Workflow_Steps
        (workflow_version_id, step_order, step_name, description, is_required, created_at)
    VALUES
        (@verId, 1, N'Trưởng phòng duyệt', N'Trưởng phòng của người tạo phê duyệt gói.', 1, @now);
    SET @stepId = SCOPE_IDENTITY();

    -- Nguồn người duyệt = bảng Thẩm quyền phê duyệt, cấp 1 (Trưởng phòng).
    INSERT INTO dbo.Workflow_Step_Approver_Rules
        (workflow_step_id, approver_source_type, approver_source_value, priority, created_at)
    VALUES
        (@stepId, 'APPROVAL_AUTHORITY', N'1', 0, @now);

    -- Binding GLOBAL đang hiệu lực.
    INSERT INTO dbo.Workflow_Bindings
        (workflow_version_id, process_code, scope_type, company_id, priority, effective_from, is_active, created_by, created_at)
    VALUES
        (@verId, 'ASSIGN_CARE_PACKAGE', 'GLOBAL', NULL, 0, @now, 1, @adminId, @now);

    PRINT 'Seeded ASSIGN_CARE_PACKAGE_DEFAULT workflow (def/ver/step/rule/binding).';
END
ELSE
BEGIN
    PRINT 'Skipped seeding ASSIGN_CARE_PACKAGE workflow (admin missing or already seeded).';
END
GO
