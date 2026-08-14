-- V0036__workflow_view_all_companies_permission.sql
--
-- VÁ LỖ HỔNG XEM CHÉO CÔNG TY ở endpoint GET /api/v2/workflows/instances (thêm ở Nhóm 1).
--
-- Chuỗi rủi ro đã xác minh:
--   1. WORKFLOW_VIEW khai báo data_scope = 'GLOBAL' (V0006).
--   2. Quyền từ CHUẨN PHÒNG BAN: khi kiểm ở phạm vi GLOBAL, MỌI phòng người đó thuộc đều tính
--      (PermissionEvaluator bước 6). Nên chỉ cần một mẫu phòng ban bất kỳ có WORKFLOW_VIEW là
--      thành viên phòng đó có quyền ở phạm vi toàn hệ thống.
--   3. Endpoint tra cứu hồ sơ KHÔNG lọc theo công ty — companyId chỉ là bộ lọc do người gọi tự
--      chọn, không phải ràng buộc.
--   4. Dữ liệu trả về có business_entity_label = "Tên gói — TÊN KHÁCH HÀNG (MÃ KH)" và tên
--      người đề xuất.
--   => Một nhân viên phòng CSKH của công ty A liệt kê được hồ sơ kèm TÊN KHÁCH của mọi công ty.
--      Đây là dữ liệu cá nhân, thuộc phạm vi NĐ 13/2023.
--
-- Cách vá: mặc định endpoint chỉ trả hồ sơ thuộc các công ty người dùng được phân công.
-- Ai thật sự cần nhìn xuyên công ty (quản trị hệ thống) thì phải được cấp RIÊNG quyền dưới đây —
-- tách bạch "xem quy trình" với "xem xuyên công ty" thay vì gộp làm một như trước.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'WORKFLOW_VIEW_ALL_COMPANIES')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('WORKFLOW_VIEW_ALL_COMPANIES', 'WORKFLOW', 'VIEW', 'GLOBAL', 1, 0, 0, 1,
         N'Xem hồ sơ quy trình của MỌI công ty (nhạy cảm: dữ liệu có kèm tên và mã khách hàng). Không có quyền này thì chỉ xem được hồ sơ của các công ty mình được phân công.');
GO

-- Cấp cho admin để không mất khả năng quản trị toàn hệ thống sau khi siết.
DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    SELECT @adminId, 'WORKFLOW_VIEW_ALL_COMPANIES', 'GLOBAL', 'ALLOW', SYSUTCDATETIME(), @adminId
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.User_Individual_Permissions uip
        WHERE uip.user_id = @adminId AND uip.permission_code = 'WORKFLOW_VIEW_ALL_COMPANIES');
GO
