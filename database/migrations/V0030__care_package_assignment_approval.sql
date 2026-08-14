-- V0030__care_package_assignment_approval.sql
-- Gắn phê duyệt vào luồng gán gói chăm sóc cho khách (Nhóm C):
--   - Thêm trạng thái PENDING_APPROVAL vào Customer_Care_Packages (chờ trưởng phòng duyệt)
--   - Thêm cột workflow_instance_id (liên kết hồ sơ quy trình phê duyệt)
--   - Seed mã nghiệp vụ ASSIGN_CARE_PACKAGE vào Business_Process_Catalog
-- Ghi chú: dùng GO tách batch. Migrator tự bọc toàn bộ trong 1 transaction.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 1. Cột workflow_instance_id
-- ══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Customer_Care_Packages') AND name = 'workflow_instance_id')
    ALTER TABLE dbo.Customer_Care_Packages
        ADD workflow_instance_id bigint NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CCP_workflow_instance_id')
    ALTER TABLE dbo.Customer_Care_Packages
        ADD CONSTRAINT FK_CCP_workflow_instance_id
            FOREIGN KEY (workflow_instance_id) REFERENCES dbo.Workflow_Instances (id);
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 2. Bổ sung PENDING_APPROVAL vào ràng buộc trạng thái
-- ══════════════════════════════════════════════════════════════════════════

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CCP_status')
    ALTER TABLE dbo.Customer_Care_Packages DROP CONSTRAINT CK_CCP_status;
GO

ALTER TABLE dbo.Customer_Care_Packages
    ADD CONSTRAINT CK_CCP_status
        CHECK (status IN ('PENDING_APPROVAL', 'PENDING_GRAVE', 'ACTIVE', 'EXPIRED', 'CANCELLED'));
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 3. Seed mã nghiệp vụ ASSIGN_CARE_PACKAGE
--    is_approval_required = 1: quy trình này có phê duyệt (bật/tắt thực tế qua binding).
-- ══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM dbo.Business_Process_Catalog WHERE process_code = 'ASSIGN_CARE_PACKAGE')
    INSERT INTO dbo.Business_Process_Catalog (process_code, process_name, description, is_approval_required, is_active, created_at)
    VALUES ('ASSIGN_CARE_PACKAGE', N'Gán gói dịch vụ cho khách hàng',
            N'Quy trình phê duyệt khi gán gói chăm sóc cho khách: nhân viên tạo, trưởng phòng duyệt rồi mới gán được vào mộ.',
            1, 1, SYSUTCDATETIME());
GO
