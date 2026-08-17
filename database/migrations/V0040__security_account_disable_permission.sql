-- V0040__security_account_disable_permission.sql
--
-- TÁCH "VÔ HIỆU/KHOÁ TÀI KHOẢN NGƯỜI KHÁC" thành QUYỀN RIÊNG trong ma trận phân quyền.
-- (Anh Bách 2026-08-16: ai có quyền disable thì mới được vô hiệu người khác.)
--
-- Trước đây disable/lock gác chung SECURITY_ACCOUNT_MANAGE. Nay endpoint disable + lock đòi
-- SECURITY_ACCOUNT_DISABLE (ô cấp riêng). Để KHÔNG AI mất khả năng đang có, migration cấp
-- SECURITY_ACCOUNT_DISABLE cho MỌI nơi đang có SECURITY_ACCOUNT_MANAGE (vai trò / nhóm quản trị /
-- cá nhân ALLOW). Bump policy_version để cache quyền (5 phút) nạp lại ngay.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- 1. Khai mã quyền vào danh mục (ma trận)
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'SECURITY_ACCOUNT_DISABLE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('SECURITY_ACCOUNT_DISABLE', 'SECURITY', 'DISABLE', 'GLOBAL', 1, 1, 0, 1,
         N'Vô hiệu/khoá tài khoản NGƯỜI KHÁC. Ai được cấp quyền này mới disable/lock được tài khoản người khác (không tự vô hiệu chính mình).');
GO

-- 2. Cấp cho VAI TRÒ đang có MANAGE
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT rp.role_id, 'SECURITY_ACCOUNT_DISABLE', SYSUTCDATETIME(), rp.created_by_user_id
FROM dbo.Role_Permissions rp
WHERE rp.permission_code = 'SECURITY_ACCOUNT_MANAGE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions x WHERE x.role_id = rp.role_id AND x.permission_code = 'SECURITY_ACCOUNT_DISABLE');
GO

-- 3. Cấp cho NHÓM QUẢN TRỊ đang có MANAGE
INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code, created_at, created_by_user_id)
SELECT gp.admin_group_id, 'SECURITY_ACCOUNT_DISABLE', SYSUTCDATETIME(), gp.created_by_user_id
FROM dbo.Admin_Group_Permissions gp
WHERE gp.permission_code = 'SECURITY_ACCOUNT_MANAGE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions x WHERE x.admin_group_id = gp.admin_group_id AND x.permission_code = 'SECURITY_ACCOUNT_DISABLE');
GO

-- 4. Cấp cho CÁ NHÂN đang có MANAGE (ALLOW còn hiệu lực) — cùng phạm vi (GLOBAL/COMPANY)
INSERT INTO dbo.User_Individual_Permissions
    (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, created_at, created_by_user_id)
SELECT uip.user_id, 'SECURITY_ACCOUNT_DISABLE', uip.scope_type, uip.company_id, 'ALLOW', 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME(), uip.created_by_user_id
FROM dbo.User_Individual_Permissions uip
WHERE uip.permission_code = 'SECURITY_ACCOUNT_MANAGE'
  AND uip.grant_type = 'ALLOW'
  AND uip.assignment_status = 'ACTIVE'
  AND NOT EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions x WHERE x.user_id = uip.user_id AND x.permission_code = 'SECURITY_ACCOUNT_DISABLE');
GO

-- 5. Bump policy_version để cache quyền nạp lại ngay (không phải chờ 5 phút)
UPDATE dbo.Authorization_Policy_State SET policy_version = policy_version + 1, updated_at = SYSUTCDATETIME() WHERE id = 1;
GO
