-- V0048__relationship_kind_manage_permission.sql
--
-- Quyền QUẢN LÝ DANH MỤC loại quan hệ gia đình (Relationship_Kinds): thêm/sửa/xoá loại quan hệ
-- (vd 'Mẹ kế'), nhãn theo giới tính, cặp nghịch đảo. Là CẤU HÌNH HỆ THỐNG dùng chung, không phải
-- dữ liệu cá nhân ⇒ data_scope = GLOBAL, is_sensitive = 0.
--
-- Cấp cho MỌI nơi đang có SYSTEM_SETTING_MANAGE (người quản trị cấu hình hệ thống). Bump policy_version.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'RELATIONSHIP_KIND_MANAGE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('RELATIONSHIP_KIND_MANAGE', 'SYSTEM', 'RELATIONSHIP_KIND_MANAGE', 'GLOBAL', 0, 0, 0, 1,
         N'Quản lý danh mục loại quan hệ gia đình (thêm/sửa/xoá loại + nhãn theo giới tính + cặp nghịch đảo).');
GO

-- Cấp cho VAI TRÒ đang có SYSTEM_SETTING_MANAGE
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT rp.role_id, 'RELATIONSHIP_KIND_MANAGE', SYSUTCDATETIME(), rp.created_by_user_id
FROM dbo.Role_Permissions rp
WHERE rp.permission_code = 'SYSTEM_SETTING_MANAGE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions x WHERE x.role_id = rp.role_id AND x.permission_code = 'RELATIONSHIP_KIND_MANAGE');
GO

-- Cấp cho NHÓM QUẢN TRỊ đang có SYSTEM_SETTING_MANAGE
INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code, created_at, created_by_user_id)
SELECT gp.admin_group_id, 'RELATIONSHIP_KIND_MANAGE', SYSUTCDATETIME(), gp.created_by_user_id
FROM dbo.Admin_Group_Permissions gp
WHERE gp.permission_code = 'SYSTEM_SETTING_MANAGE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions x WHERE x.admin_group_id = gp.admin_group_id AND x.permission_code = 'RELATIONSHIP_KIND_MANAGE');
GO

-- Cấp cho CÁ NHÂN đang có SYSTEM_SETTING_MANAGE (ALLOW còn hiệu lực)
INSERT INTO dbo.User_Individual_Permissions
    (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, created_at, created_by_user_id)
SELECT uip.user_id, 'RELATIONSHIP_KIND_MANAGE', uip.scope_type, uip.company_id, 'ALLOW', 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME(), uip.created_by_user_id
FROM dbo.User_Individual_Permissions uip
WHERE uip.permission_code = 'SYSTEM_SETTING_MANAGE'
  AND uip.grant_type = 'ALLOW'
  AND uip.assignment_status = 'ACTIVE'
  AND NOT EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions x WHERE x.user_id = uip.user_id AND x.permission_code = 'RELATIONSHIP_KIND_MANAGE');
GO

UPDATE dbo.Authorization_Policy_State SET policy_version = policy_version + 1, updated_at = SYSUTCDATETIME() WHERE id = 1;
GO
