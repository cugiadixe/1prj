-- V0055__settings_view_permissions.sql
--
-- Thêm quyền XEM (VIEW) cho các mục CÀI ĐẶT, tách khỏi quyền SỬA (MANAGE) đã có:
--   RELATIONSHIP_KIND_VIEW, TAG_VIEW, SYSTEM_SETTING_VIEW, SYSTEM_HEALTH_VIEW.
-- Mục tiêu: gom các mục cấu hình vào 1 menu "Cài đặt hệ thống" và phân quyền view/edit rõ ràng.
--
-- Nguyên tắc KHÔNG làm mất quyền: mỗi VIEW được NHÂN BẢN từ người/role/nhóm đang có MANAGE tương
-- ứng (SYSTEM_HEALTH_VIEW nhân từ SYSTEM_SETTING_MANAGE vì trang Hệ thống trước đây không gác quyền).
-- Ngoài ra cấp RELATIONSHIP_KIND_VIEW cho role NHAN_VIEN + TRUONG_PHONG: trước đây dropdown "loại
-- quan hệ" (khi khai quan hệ) đòi RELATIONSHIP_KIND_MANAGE nên nhân viên KHÔNG tải được — nay GET
-- endpoint hạ xuống VIEW, cấp VIEW cho 2 role để vá.
--
-- Idempotent: chỉ chèn dòng chưa có.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

-- 1) Danh mục quyền mới
INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description, created_at)
SELECT v.permission_code, v.module_code, v.action_code, v.data_scope, v.is_sensitive, 0, 1, 1, v.description, SYSUTCDATETIME()
FROM (VALUES
    ('RELATIONSHIP_KIND_VIEW', 'SYSTEM', 'VIEW', 'GLOBAL',  0, N'Xem danh mục loại quan hệ'),
    ('TAG_VIEW',               'TAG',    'VIEW', 'COMPANY', 0, N'Xem danh mục thẻ (trang quản lý)'),
    ('SYSTEM_SETTING_VIEW',    'SYSTEM', 'VIEW', 'GLOBAL',  1, N'Xem cấu hình hệ thống (không sửa)'),
    ('SYSTEM_HEALTH_VIEW',     'SYSTEM', 'VIEW', 'GLOBAL',  0, N'Xem trang tình trạng hệ thống')
) v(permission_code, module_code, action_code, data_scope, is_sensitive, description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions p WHERE p.permission_code = v.permission_code);

-- Bảng cặp (VIEW ← nhân bản từ MANAGE)
DECLARE @pairs TABLE (view_code VARCHAR(100), manage_code VARCHAR(100));
INSERT INTO @pairs VALUES
    ('RELATIONSHIP_KIND_VIEW', 'RELATIONSHIP_KIND_MANAGE'),
    ('TAG_VIEW',               'TAG_MANAGE'),
    ('SYSTEM_SETTING_VIEW',    'SYSTEM_SETTING_MANAGE'),
    ('SYSTEM_HEALTH_VIEW',     'SYSTEM_SETTING_MANAGE');

-- 2a) Nhân bản grant ở ROLE
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT rp.role_id, pr.view_code, SYSUTCDATETIME(), NULL
FROM dbo.Role_Permissions rp
JOIN @pairs pr ON pr.manage_code = rp.permission_code
WHERE NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions x WHERE x.role_id = rp.role_id AND x.permission_code = pr.view_code);

-- 2b) Nhân bản grant CÁ NHÂN (chỉ dòng ALLOW/ACTIVE), giữ nguyên scope + công ty
INSERT INTO dbo.User_Individual_Permissions
    (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, created_at, created_by_user_id)
SELECT uip.user_id, pr.view_code, uip.scope_type, uip.company_id, uip.grant_type, uip.assignment_status, uip.effective_from, SYSUTCDATETIME(), NULL
FROM dbo.User_Individual_Permissions uip
JOIN @pairs pr ON pr.manage_code = uip.permission_code
WHERE uip.grant_type = 'ALLOW' AND uip.assignment_status = 'ACTIVE'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.User_Individual_Permissions x
      WHERE x.user_id = uip.user_id AND x.permission_code = pr.view_code
        AND x.scope_type = uip.scope_type AND ISNULL(x.company_id, -1) = ISNULL(uip.company_id, -1));

-- 2c) Nhân bản grant ở NHÓM QUẢN TRỊ
INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code, created_at, created_by_user_id)
SELECT agp.admin_group_id, pr.view_code, SYSUTCDATETIME(), NULL
FROM dbo.Admin_Group_Permissions agp
JOIN @pairs pr ON pr.manage_code = agp.permission_code
WHERE NOT EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions x WHERE x.admin_group_id = agp.admin_group_id AND x.permission_code = pr.view_code);

-- 3) Cấp RELATIONSHIP_KIND_VIEW cho role Nhân viên + Trưởng phòng (để tải dropdown loại quan hệ)
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT r.id, 'RELATIONSHIP_KIND_VIEW', SYSUTCDATETIME(), NULL
FROM dbo.Roles r
WHERE r.role_code IN ('NHAN_VIEN', 'TRUONG_PHONG')
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions x WHERE x.role_id = r.id AND x.permission_code = 'RELATIONSHIP_KIND_VIEW');
