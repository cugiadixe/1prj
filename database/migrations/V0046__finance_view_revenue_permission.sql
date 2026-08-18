-- V0046__finance_view_revenue_permission.sql
--
-- TÁCH "XEM DOANH THU" trên dashboard thành QUYỀN RIÊNG trong ma trận phân quyền.
-- (Anh Bách 2026-08-18: ai vào được dashboard chưa chắc được xem tiền.)
--
-- Trước đây dashboard chỉ gác CUSTOMER_VIEW_BASIC — ai xem được dashboard là thấy luôn KPI
-- "Doanh thu" + biểu đồ doanh thu 6 tháng. Nay hai widget doanh thu đòi FINANCE_VIEW_REVENUE
-- (ô cấp riêng, is_sensitive). Không có quyền này thì backend KHÔNG trả số doanh thu và frontend
-- thay bằng widget khác (Tổng dịch vụ + Gói chăm sóc bán theo tháng).
--
-- Để KHÔNG AI mất khả năng đang có, migration cấp FINANCE_VIEW_REVENUE cho MỌI nơi đang có
-- CUSTOMER_VIEW_BASIC (vai trò / nhóm quản trị / cá nhân ALLOW). Sau đó, muốn GIẤU doanh thu với
-- ai thì GỠ FINANCE_VIEW_REVENUE khỏi vai trò/người đó trong ma trận phân quyền — họ sẽ tự động
-- thấy dashboard thay thế. Bump policy_version để cache quyền (5 phút) nạp lại ngay.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- 1. Khai mã quyền vào danh mục (ma trận). data_scope = COMPANY (doanh thu tính theo công ty).
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'FINANCE_VIEW_REVENUE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('FINANCE_VIEW_REVENUE', 'FINANCE', 'VIEW_REVENUE', 'COMPANY', 1, 0, 0, 1,
         N'Xem số liệu doanh thu trên dashboard (KPI Doanh thu + biểu đồ doanh thu theo tháng). Không có quyền này thì dashboard thay hai widget doanh thu bằng widget khác.');
GO

-- 2. Cấp cho VAI TRÒ đang có CUSTOMER_VIEW_BASIC
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT rp.role_id, 'FINANCE_VIEW_REVENUE', SYSUTCDATETIME(), rp.created_by_user_id
FROM dbo.Role_Permissions rp
WHERE rp.permission_code = 'CUSTOMER_VIEW_BASIC'
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions x WHERE x.role_id = rp.role_id AND x.permission_code = 'FINANCE_VIEW_REVENUE');
GO

-- 3. Cấp cho NHÓM QUẢN TRỊ đang có CUSTOMER_VIEW_BASIC
INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code, created_at, created_by_user_id)
SELECT gp.admin_group_id, 'FINANCE_VIEW_REVENUE', SYSUTCDATETIME(), gp.created_by_user_id
FROM dbo.Admin_Group_Permissions gp
WHERE gp.permission_code = 'CUSTOMER_VIEW_BASIC'
  AND NOT EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions x WHERE x.admin_group_id = gp.admin_group_id AND x.permission_code = 'FINANCE_VIEW_REVENUE');
GO

-- 4. Cấp cho CÁ NHÂN đang có CUSTOMER_VIEW_BASIC (ALLOW còn hiệu lực) — cùng phạm vi (GLOBAL/COMPANY)
INSERT INTO dbo.User_Individual_Permissions
    (user_id, permission_code, scope_type, company_id, grant_type, assignment_status, effective_from, created_at, created_by_user_id)
SELECT uip.user_id, 'FINANCE_VIEW_REVENUE', uip.scope_type, uip.company_id, 'ALLOW', 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME(), uip.created_by_user_id
FROM dbo.User_Individual_Permissions uip
WHERE uip.permission_code = 'CUSTOMER_VIEW_BASIC'
  AND uip.grant_type = 'ALLOW'
  AND uip.assignment_status = 'ACTIVE'
  AND NOT EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions x WHERE x.user_id = uip.user_id AND x.permission_code = 'FINANCE_VIEW_REVENUE');
GO

-- 5. Bump policy_version để cache quyền nạp lại ngay (không phải chờ 5 phút)
UPDATE dbo.Authorization_Policy_State SET policy_version = policy_version + 1, updated_at = SYSUTCDATETIME() WHERE id = 1;
GO
