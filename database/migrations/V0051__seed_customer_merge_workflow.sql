-- V0051__seed_customer_merge_workflow.sql
--
-- Seed quy trình duyệt GỘP KHÁCH HÀNG TRÙNG (CUSTOMER_MERGE_DUPLICATE) — 1 bước, khuôn V0039.
--
-- Bối cảnh: V0035 đã thêm mã quy trình + trường điều kiện cho CUSTOMER_MERGE_DUPLICATE, nhưng
-- CHƯA có Workflow_Definitions/Versions/Steps/Bindings. Không có binding thì tạo hồ sơ gộp sẽ
-- ném WF_NO_VALID_BINDING — tức yêu cầu gộp kẹt vĩnh viễn ở DRAFT, không ai duyệt được.
-- (Đó chính là hiện trạng anh Bách thấy: "Đã lên bản draft, ai duyệt?")
--
-- Luật "gộp thì luôn phải duyệt" nằm ở TẦNG SERVICE (mọi yêu cầu gộp đều mở workflow khi submit),
-- nên binding KHÔNG cần điều kiện — chỉ cần quy trình 1 bước như CARD_REPRINT/ASSIGN_CARE_PACKAGE.
--
-- ⚠️ PLACEHOLDER: người duyệt TẠM đặt = admin để pilot chạy được. Cấu hình lại thành người
--    duyệt thật (VD Trưởng phòng KD / người được uỷ quyền) qua UI cấu hình quy trình.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.Business_Process_Catalog WHERE process_code = 'CUSTOMER_MERGE_DUPLICATE')
   AND NOT EXISTS (SELECT 1 FROM dbo.Workflow_Definitions WHERE definition_code = 'CUSTOMER_MERGE_DEFAULT')
BEGIN
    DECLARE @now datetime2(3) = SYSUTCDATETIME();
    DECLARE @defId BIGINT, @verId BIGINT, @stepId BIGINT;

    INSERT INTO dbo.Workflow_Definitions
        (definition_code, definition_name, description, process_code, is_active, created_by, created_at)
    VALUES
        ('CUSTOMER_MERGE_DEFAULT', N'Duyệt gộp khách hàng trùng',
         N'Gộp hai hồ sơ khách hàng trùng nhau cần 1 cấp duyệt trước khi dồn dữ liệu về hồ sơ đích.',
         'CUSTOMER_MERGE_DUPLICATE', 1, @adminId, @now);
    SET @defId = SCOPE_IDENTITY();

    INSERT INTO dbo.Workflow_Definition_Versions
        (workflow_definition_id, version_number, version_status, effective_from, published_at, published_by, created_by, created_at)
    VALUES
        (@defId, 1, 'ACTIVE', @now, @now, @adminId, @adminId, @now);
    SET @verId = SCOPE_IDENTITY();

    INSERT INTO dbo.Workflow_Steps
        (workflow_version_id, step_order, step_name, description, is_required, created_at)
    VALUES
        (@verId, 1, N'Duyệt gộp khách hàng',
         N'Người có thẩm quyền duyệt việc gộp hồ sơ nguồn vào hồ sơ đích (không thể hoàn tác sau khi thực thi).', 1, @now);
    SET @stepId = SCOPE_IDENTITY();

    -- ⚠️ PLACEHOLDER người duyệt = admin. Cấu hình lại qua UI cấu hình quy trình.
    INSERT INTO dbo.Workflow_Step_Approver_Rules
        (workflow_step_id, approver_source_type, approver_source_value, priority, created_at)
    VALUES
        (@stepId, 'SPECIFIC_USER', CAST(@adminId AS nvarchar(50)), 0, @now);

    INSERT INTO dbo.Workflow_Bindings
        (workflow_version_id, process_code, scope_type, company_id, priority, effective_from, is_active, created_by, created_at)
    VALUES
        (@verId, 'CUSTOMER_MERGE_DUPLICATE', 'GLOBAL', NULL, 0, @now, 1, @adminId, @now);

    PRINT 'Seeded CUSTOMER_MERGE_DEFAULT workflow (def/ver/step/rule/binding).';
END
ELSE
    PRINT 'Skipped CUSTOMER_MERGE_DUPLICATE workflow seed (admin missing, process missing, or already seeded).';
GO
