-- V0057__card_watermark_manage_permission.sql
--
-- Tách quyền CARD_WATERMARK_MANAGE (quản lý THƯ VIỆN hoa văn thẻ) khỏi CARD_ISSUE (phát thẻ).
-- Mục tiêu: đưa "Hoa văn thẻ" vào role Quản trị cấu hình mà không phải kèm quyền phát thẻ.
--
-- Controller card-watermarks đã đổi sang đòi CARD_WATERMARK_MANAGE. Để KHÔNG ai mất quyền, nhân bản
-- grant từ CARD_ISSUE sang CARD_WATERMARK_MANAGE (role + cá nhân ALLOW/ACTIVE + nhóm admin). Ngoài
-- ra cấp cho role QUAN_TRI_CAU_HINH (V0056) để quản trị cấu hình quản lý được hoa văn.
--
-- Idempotent.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

-- 1) Danh mục quyền mới (CARD / COMPANY)
INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description, created_at)
SELECT 'CARD_WATERMARK_MANAGE', 'CARD', 'MANAGE', 'COMPANY', 0, 0, 1, 1, N'Quản lý thư viện hoa văn thẻ', SYSUTCDATETIME()
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions p WHERE p.permission_code = 'CARD_WATERMARK_MANAGE');

-- 2a) Nhân bản grant ROLE từ CARD_ISSUE
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT rp.role_id, 'CARD_WATERMARK_MANAGE', SYSUTCDATETIME(), NULL
FROM dbo.Role_Permissions rp
WHERE rp.permission_code = 'CARD_ISSUE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions x WHERE x.role_id = rp.role_id AND x.permission_code = 'CARD_WATERMARK_MANAGE');

-- 2b) Nhân bản grant CÁ NHÂN (ALLOW/ACTIVE), giữ scope + công ty
INSERT INTO dbo.User_Individual_Permissions
    (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, created_at, created_by_user_id)
SELECT uip.user_id, 'CARD_WATERMARK_MANAGE', uip.scope_type, uip.company_id, uip.grant_type, uip.assignment_status, uip.effective_from, SYSUTCDATETIME(), NULL
FROM dbo.User_Individual_Permissions uip
WHERE uip.permission_code = 'CARD_ISSUE' AND uip.grant_type = 'ALLOW' AND uip.assignment_status = 'ACTIVE'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.User_Individual_Permissions x
      WHERE x.user_id = uip.user_id AND x.permission_code = 'CARD_WATERMARK_MANAGE'
        AND x.scope_type = uip.scope_type AND ISNULL(x.company_id, -1) = ISNULL(uip.company_id, -1));

-- 2c) Nhân bản grant NHÓM QUẢN TRỊ
INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code, created_at, created_by_user_id)
SELECT agp.admin_group_id, 'CARD_WATERMARK_MANAGE', SYSUTCDATETIME(), NULL
FROM dbo.Admin_Group_Permissions agp
WHERE agp.permission_code = 'CARD_ISSUE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions x WHERE x.admin_group_id = agp.admin_group_id AND x.permission_code = 'CARD_WATERMARK_MANAGE');

-- 3) Cấp cho role Quản trị cấu hình
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT r.id, 'CARD_WATERMARK_MANAGE', SYSUTCDATETIME(), NULL
FROM dbo.Roles r
WHERE r.role_code = 'QUAN_TRI_CAU_HINH'
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions x WHERE x.role_id = r.id AND x.permission_code = 'CARD_WATERMARK_MANAGE');
