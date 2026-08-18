-- V0047__customer_relationship_manage_permission.sql
--
-- Quyền KHAI/XOÁ QUAN HỆ GIA ĐÌNH giữa hai khách hàng (đồ thị Customer_Relationships).
-- Nền tảng P1 cho việc gán cốt vào mộ: chỉ người có quan hệ gia đình với chủ mộ mới được đặt cốt,
-- nên trước hết phải có đường khai quan hệ (bảng V0022 trước đây chỉ được ĐỌC, không có đường ghi).
--
-- Là dữ liệu gia đình nhạy cảm (NĐ13) ⇒ is_sensitive = 1. data_scope = COMPANY (tầng service đòi
-- CẢ HAI khách đều thuộc phạm vi người thao tác). Để không đổi khả năng hiện có, cấp quyền này cho
-- MỌI nơi đang có CUSTOMER_MASTER_UPDATE (người sửa dữ liệu gốc khách hàng). Bump policy_version để
-- cache quyền nạp lại ngay.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- 1. Khai mã quyền vào danh mục (ma trận)
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CUSTOMER_RELATIONSHIP_MANAGE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CUSTOMER_RELATIONSHIP_MANAGE', 'CUSTOMER', 'RELATIONSHIP_MANAGE', 'COMPANY', 1, 0, 0, 1,
         N'Khai/xoá quan hệ gia đình giữa hai khách hàng. Nền tảng để gán cốt vào mộ theo quan hệ với chủ mộ.');
GO

-- 2. Cấp cho VAI TRÒ đang có CUSTOMER_MASTER_UPDATE
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT rp.role_id, 'CUSTOMER_RELATIONSHIP_MANAGE', SYSUTCDATETIME(), rp.created_by_user_id
FROM dbo.Role_Permissions rp
WHERE rp.permission_code = 'CUSTOMER_MASTER_UPDATE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions x WHERE x.role_id = rp.role_id AND x.permission_code = 'CUSTOMER_RELATIONSHIP_MANAGE');
GO

-- 3. Cấp cho NHÓM QUẢN TRỊ đang có CUSTOMER_MASTER_UPDATE
INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code, created_at, created_by_user_id)
SELECT gp.admin_group_id, 'CUSTOMER_RELATIONSHIP_MANAGE', SYSUTCDATETIME(), gp.created_by_user_id
FROM dbo.Admin_Group_Permissions gp
WHERE gp.permission_code = 'CUSTOMER_MASTER_UPDATE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions x WHERE x.admin_group_id = gp.admin_group_id AND x.permission_code = 'CUSTOMER_RELATIONSHIP_MANAGE');
GO

-- 4. Cấp cho CÁ NHÂN đang có CUSTOMER_MASTER_UPDATE (ALLOW còn hiệu lực) — cùng phạm vi
INSERT INTO dbo.User_Individual_Permissions
    (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, created_at, created_by_user_id)
SELECT uip.user_id, 'CUSTOMER_RELATIONSHIP_MANAGE', uip.scope_type, uip.company_id, 'ALLOW', 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME(), uip.created_by_user_id
FROM dbo.User_Individual_Permissions uip
WHERE uip.permission_code = 'CUSTOMER_MASTER_UPDATE'
  AND uip.grant_type = 'ALLOW'
  AND uip.assignment_status = 'ACTIVE'
  AND NOT EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions x WHERE x.user_id = uip.user_id AND x.permission_code = 'CUSTOMER_RELATIONSHIP_MANAGE');
GO

-- 5. Bump policy_version để cache quyền nạp lại ngay
UPDATE dbo.Authorization_Policy_State SET policy_version = policy_version + 1, updated_at = SYSUTCDATETIME() WHERE id = 1;
GO
