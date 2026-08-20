-- V0056__role_config_admin.sql
--
-- Tạo role "Quản trị cấu hình" (QUAN_TRI_CAU_HINH) gom các quyền cấu hình hệ thống (nhóm menu
-- "Cài đặt hệ thống") để có thể GÁN cho người khác ngoài admin cá nhân. KHÔNG gán user nào ở đây —
-- việc gán người do admin làm qua UI (Bảo mật → Vai trò / gán vai trò cho người dùng).
--
-- Gồm: Loại quan hệ, Thẻ, Gói dịch vụ, Cấu hình lưu trữ, Hệ thống (cả VIEW + MANAGE).
-- KHÔNG gồm: Hoa văn thẻ (gác CARD_ISSUE = quyền tác nghiệp phát thẻ, tránh lẫn); Bảo mật / Tổ
-- chức / Quy trình / Thẩm quyền phê duyệt (nhạy cảm — giữ cho admin).
--
-- Idempotent: tạo role nếu chưa có; chỉ gán quyền chưa có; chỉ gán quyền đang active.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

-- 1) Tạo role (GLOBAL) nếu chưa có
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE role_code = 'QUAN_TRI_CAU_HINH')
BEGIN
    INSERT INTO dbo.Roles (role_code, name, description, scope_type, company_id, is_active, created_at)
    VALUES ('QUAN_TRI_CAU_HINH', N'Quản trị cấu hình',
            N'Quản lý danh mục & cấu hình hệ thống (loại quan hệ, thẻ, gói dịch vụ, lưu trữ, tình trạng hệ thống).',
            'GLOBAL', NULL, 1, SYSUTCDATETIME());
END

DECLARE @rid BIGINT = (SELECT id FROM dbo.Roles WHERE role_code = 'QUAN_TRI_CAU_HINH');

-- 2) Bộ quyền của role
DECLARE @perms TABLE (code VARCHAR(100) PRIMARY KEY);
INSERT INTO @perms (code) VALUES
    ('RELATIONSHIP_KIND_VIEW'), ('RELATIONSHIP_KIND_MANAGE'),
    ('TAG_VIEW'), ('TAG_MANAGE'),
    ('SERVICE_TYPE_VIEW'), ('SERVICE_TYPE_MANAGE'),
    ('SYSTEM_SETTING_VIEW'), ('SYSTEM_SETTING_MANAGE'),
    ('SYSTEM_HEALTH_VIEW');

INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT @rid, p.permission_code, SYSUTCDATETIME(), NULL
FROM dbo.Permissions p
JOIN @perms m ON m.code = p.permission_code
WHERE @rid IS NOT NULL AND p.is_active = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions rp
                  WHERE rp.role_id = @rid AND rp.permission_code = p.permission_code);
