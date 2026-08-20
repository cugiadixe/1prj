-- V0054__seed_role_permissions_truongphong_nhanvien.sql
--
-- Gán bộ quyền theo CHỨC DANH cho 2 role có sẵn: NHAN_VIEN (Nhân viên) và TRUONG_PHONG
-- (Trưởng phòng). Hai role này đã tồn tại (GLOBAL) và đã gán ~100 người nhưng trước đây
-- 0 quyền → gán mà vô tác dụng. Migration này nạp quyền nền; sau đó admin tinh chỉnh lẻ
-- qua UI Bảo mật → Vai trò (/security/roles).
--
-- Quyết định (anh Bách 2026-08-20):
--   • Cô lập dữ liệu theo CÔNG TY (quyền data_scope=COMPANY + UserCompanyAssignment sẵn có).
--     KHÔNG cô lập theo phòng ban (hệ chưa hỗ trợ data_scope=DEPARTMENT).
--   • Xác nhận TIỀN (thanh toán/đối soát) CHỈ Trưởng phòng; Nhân viên chỉ tạo nháp/lập.
--
-- KHÔNG cấp cho 2 role: quản trị bảo mật/tổ chức, cấu hình/publish quy trình, cài đặt hệ
-- thống, quản lý danh mục loại dịch vụ/quan hệ, thẩm quyền phê duyệt — giữ cho admin.
--
-- Idempotent: chỉ chèn dòng chưa có; chỉ chèn quyền đang active (JOIN Permissions) nên mã sai
-- sẽ bị bỏ qua thay vì tạo dòng rác. Resolve role theo role_code (không hardcode id).

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @nv BIGINT = (SELECT id FROM dbo.Roles WHERE role_code = 'NHAN_VIEN');
DECLARE @tp BIGINT = (SELECT id FROM dbo.Roles WHERE role_code = 'TRUONG_PHONG');

-- Bộ quyền NỀN của Nhân viên (tác nghiệp trong công ty mình).
DECLARE @nvPerms TABLE (code VARCHAR(100) PRIMARY KEY);
INSERT INTO @nvPerms (code) VALUES
    -- Khách hàng
    ('CUSTOMER_VIEW_BASIC'), ('CUSTOMER_VIEW_SENSITIVE'), ('CUSTOMER_CREATE_FINAL'),
    ('CUSTOMER_CHANGE_REQUEST_CREATE'), ('CUSTOMER_RELATIONSHIP_MANAGE'),
    ('CUSTOMER_MERGE_REQUEST_CREATE'), ('CUSTOMER_MERGE_REQUEST_VIEW'),
    -- Mộ
    ('GRAVE_VIEW'), ('GRAVE_CREATE'), ('GRAVE_UPDATE'), ('GRAVE_OCCUPANT_MANAGE'),
    ('GRAVE_EMERGENCY_CONTACT_MANAGE'), ('GRAVE_ATTACHMENT_MANAGE'),
    -- Dịch vụ (xin vượt giá, không duyệt)
    ('SERVICE_VIEW'), ('SERVICE_TYPE_VIEW'), ('SERVICE_CREATE_STANDARD'),
    ('SERVICE_RENEW_STANDARD'), ('SERVICE_PRICE_OVERRIDE_REQUEST'),
    -- Gói chăm sóc
    ('CARE_PACKAGE_VIEW'), ('CARE_PACKAGE_CREATE'), ('CARE_PACKAGE_CREATE_PAYMENT'),
    ('CUSTOMER_CARE_PACKAGE_VIEW'), ('CUSTOMER_CARE_PACKAGE_MANAGE'),
    -- Thanh toán (chỉ tạo nháp + in; xác nhận để Trưởng phòng)
    ('PAYMENT_VIEW'), ('PAYMENT_CREATE_DRAFT'), ('PAYMENT_PRINT'),
    -- Thẻ (tạo/đánh dấu in; duyệt in lại để Trưởng phòng)
    ('CARD_ISSUE'), ('CARD_REPRINT_REQUEST_CREATE'), ('CARD_REPRINT_REQUEST_VIEW'),
    ('CARD_REPRINT_REQUEST_MARK_PRINTED'),
    -- Đối soát (lập; xác nhận để Trưởng phòng)
    ('RECONCILIATION_VIEW'), ('RECONCILIATION_PREPARE'),
    -- Khác
    ('TAG_MANAGE'), ('WORKFLOW_VIEW'),
    ('ORGANIZATION_COMPANY_VIEW'), ('ORGANIZATION_DEPARTMENT_VIEW');

-- Quyền THÊM của Trưởng phòng (duyệt + giám sát + xác nhận tiền). Cộng dồn với bộ Nhân viên.
DECLARE @tpExtra TABLE (code VARCHAR(100) PRIMARY KEY);
INSERT INTO @tpExtra (code) VALUES
    -- Duyệt/từ chối
    ('CARE_PACKAGE_APPROVE'), ('CARE_PACKAGE_REJECT'),
    ('CARD_REPRINT_APPROVE'), ('CARD_REPRINT_REQUEST_REJECT'),
    ('SERVICE_PRICE_OVERRIDE_APPROVE'),
    ('WORKFLOW_REJECT'), ('WORKFLOW_REASSIGN_PENDING'),
    -- Giám sát + xác nhận tiền
    ('FINANCE_VIEW_REVENUE'),
    ('PAYMENT_CONFIRM'), ('PAYMENT_CORRECT_CONFIRMED'),
    ('RECONCILIATION_CONFIRM'),
    ('CUSTOMER_MASTER_UPDATE'), ('CUSTOMER_MERGE_REQUEST_ADMIN_VIEW'),
    ('GRAVE_TRANSFER_OWNERSHIP');

-- Gán bộ Nhân viên cho role NHAN_VIEN
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT @nv, p.permission_code, SYSUTCDATETIME(), NULL
FROM dbo.Permissions p
JOIN @nvPerms n ON n.code = p.permission_code
WHERE @nv IS NOT NULL AND p.is_active = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions rp
                  WHERE rp.role_id = @nv AND rp.permission_code = p.permission_code);

-- Gán (bộ Nhân viên + quyền thêm) cho role TRUONG_PHONG
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT @tp, p.permission_code, SYSUTCDATETIME(), NULL
FROM dbo.Permissions p
JOIN (SELECT code FROM @nvPerms UNION SELECT code FROM @tpExtra) t ON t.code = p.permission_code
WHERE @tp IS NOT NULL AND p.is_active = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions rp
                  WHERE rp.role_id = @tp AND rp.permission_code = p.permission_code);
