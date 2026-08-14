-- V0032__permission_descriptions_vietnamese.sql
-- Việt hoá mô tả quyền trong danh mục Permissions (nhiều mô tả đang tiếng Anh; TAG_MANAGE bị hỏng font).
-- Mô tả này hiển thị ở dropdown "Cấp quyền cá nhân" và danh sách quyền — sửa 1 nguồn, đúng mọi nơi.
-- Các mô tả vốn đã đúng tiếng Việt (GRAVE_*, CUSTOMER_CARE_PACKAGE_*, APPROVAL_AUTHORITY_MANAGE) giữ nguyên.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

UPDATE dbo.Permissions SET description = N'Phê duyệt yêu cầu in lại thẻ.' WHERE permission_code = 'CARD_REPRINT_APPROVE';
UPDATE dbo.Permissions SET description = N'Tạo yêu cầu in lại thẻ.' WHERE permission_code = 'CARD_REPRINT_REQUEST_CREATE';
UPDATE dbo.Permissions SET description = N'Đánh dấu yêu cầu in lại thẻ đã in.' WHERE permission_code = 'CARD_REPRINT_REQUEST_MARK_PRINTED';
UPDATE dbo.Permissions SET description = N'Từ chối yêu cầu in lại thẻ.' WHERE permission_code = 'CARD_REPRINT_REQUEST_REJECT';
UPDATE dbo.Permissions SET description = N'Xem yêu cầu in lại thẻ trong công ty được phân.' WHERE permission_code = 'CARD_REPRINT_REQUEST_VIEW';

UPDATE dbo.Permissions SET description = N'Phê duyệt yêu cầu gói chăm sóc cần duyệt.' WHERE permission_code = 'CARE_PACKAGE_APPROVE';
UPDATE dbo.Permissions SET description = N'Tạo gói chăm sóc.' WHERE permission_code = 'CARE_PACKAGE_CREATE';
UPDATE dbo.Permissions SET description = N'Tạo thanh toán cho yêu cầu gói chăm sóc đủ điều kiện.' WHERE permission_code = 'CARE_PACKAGE_CREATE_PAYMENT';
UPDATE dbo.Permissions SET description = N'Từ chối yêu cầu gói chăm sóc cần duyệt.' WHERE permission_code = 'CARE_PACKAGE_REJECT';
UPDATE dbo.Permissions SET description = N'Xem gói chăm sóc.' WHERE permission_code = 'CARE_PACKAGE_VIEW';

UPDATE dbo.Permissions SET description = N'Gửi đề xuất tạo/thay đổi khách hàng.' WHERE permission_code = 'CUSTOMER_CHANGE_REQUEST_CREATE';
UPDATE dbo.Permissions SET description = N'Tạo trực tiếp hồ sơ khách hàng (thao tác quản trị).' WHERE permission_code = 'CUSTOMER_CREATE_FINAL';
UPDATE dbo.Permissions SET description = N'Cập nhật trực tiếp dữ liệu gốc khách hàng (thao tác quản trị, bắt buộc nêu lý do).' WHERE permission_code = 'CUSTOMER_MASTER_UPDATE';
UPDATE dbo.Permissions SET description = N'Thực thi gộp khách hàng trùng.' WHERE permission_code = 'CUSTOMER_MERGE_EXECUTE';
UPDATE dbo.Permissions SET description = N'Xem tất cả yêu cầu gộp khách hàng.' WHERE permission_code = 'CUSTOMER_MERGE_REQUEST_ADMIN_VIEW';
UPDATE dbo.Permissions SET description = N'Tạo yêu cầu gộp khách hàng.' WHERE permission_code = 'CUSTOMER_MERGE_REQUEST_CREATE';
UPDATE dbo.Permissions SET description = N'Xem yêu cầu gộp khách hàng.' WHERE permission_code = 'CUSTOMER_MERGE_REQUEST_VIEW';
UPDATE dbo.Permissions SET description = N'Xem danh sách và thông tin cơ bản của khách hàng.' WHERE permission_code = 'CUSTOMER_VIEW_BASIC';
UPDATE dbo.Permissions SET description = N'Xem trường nhạy cảm của khách hàng (CCCD, địa chỉ, điện thoại).' WHERE permission_code = 'CUSTOMER_VIEW_SENSITIVE';

UPDATE dbo.Permissions SET description = N'Quản lý công ty.' WHERE permission_code = 'ORGANIZATION_COMPANY_MANAGE';
UPDATE dbo.Permissions SET description = N'Xem công ty.' WHERE permission_code = 'ORGANIZATION_COMPANY_VIEW';
UPDATE dbo.Permissions SET description = N'Quản lý phòng ban.' WHERE permission_code = 'ORGANIZATION_DEPARTMENT_MANAGE';
UPDATE dbo.Permissions SET description = N'Xem phòng ban.' WHERE permission_code = 'ORGANIZATION_DEPARTMENT_VIEW';
UPDATE dbo.Permissions SET description = N'Quản lý người dùng của tổ chức.' WHERE permission_code = 'ORGANIZATION_USER_MANAGE';

UPDATE dbo.Permissions SET description = N'Xác nhận phiếu thu nháp hợp lệ.' WHERE permission_code = 'PAYMENT_CONFIRM';
UPDATE dbo.Permissions SET description = N'Điều chỉnh phiếu thu đã xác nhận theo ràng buộc chặt.' WHERE permission_code = 'PAYMENT_CORRECT_CONFIRMED';
UPDATE dbo.Permissions SET description = N'Tạo phiếu thu/hóa đơn nháp.' WHERE permission_code = 'PAYMENT_CREATE_DRAFT';
UPDATE dbo.Permissions SET description = N'In phiếu thu/hóa đơn đã xác nhận.' WHERE permission_code = 'PAYMENT_PRINT';

UPDATE dbo.Permissions SET description = N'Xác nhận đối soát.' WHERE permission_code = 'RECONCILIATION_CONFIRM';
UPDATE dbo.Permissions SET description = N'Chuẩn bị kỳ/dữ liệu đối soát.' WHERE permission_code = 'RECONCILIATION_PREPARE';

UPDATE dbo.Permissions SET description = N'Quản lý tài khoản đăng nhập và phiên.' WHERE permission_code = 'SECURITY_ACCOUNT_MANAGE';
UPDATE dbo.Permissions SET description = N'Quản lý nhóm quản trị bảo mật.' WHERE permission_code = 'SECURITY_ADMIN_GROUP_MANAGE';
UPDATE dbo.Permissions SET description = N'Xem nhóm quản trị bảo mật.' WHERE permission_code = 'SECURITY_ADMIN_GROUP_VIEW';
UPDATE dbo.Permissions SET description = N'Quản trị cấu hình bảo mật (vai trò, nhóm quản trị, quyền, phân công, quyền phòng ban, quyền hiệu dụng).' WHERE permission_code = 'SECURITY_ADMIN_MANAGE';
UPDATE dbo.Permissions SET description = N'Quản lý phân công bảo mật theo phạm vi.' WHERE permission_code = 'SECURITY_ASSIGNMENT_MANAGE';
UPDATE dbo.Permissions SET description = N'Xem nhật ký kiểm toán xác thực, phân quyền và quản trị bảo mật.' WHERE permission_code = 'SECURITY_AUDIT_VIEW';
UPDATE dbo.Permissions SET description = N'Quản lý danh mục quyền bảo mật.' WHERE permission_code = 'SECURITY_PERMISSION_MANAGE';
UPDATE dbo.Permissions SET description = N'Xem danh mục quyền bảo mật.' WHERE permission_code = 'SECURITY_PERMISSION_VIEW';
UPDATE dbo.Permissions SET description = N'Quản lý vai trò bảo mật.' WHERE permission_code = 'SECURITY_ROLE_MANAGE';
UPDATE dbo.Permissions SET description = N'Xem vai trò bảo mật.' WHERE permission_code = 'SECURITY_ROLE_VIEW';
UPDATE dbo.Permissions SET description = N'Quản lý dữ liệu người dùng bảo mật.' WHERE permission_code = 'SECURITY_USER_MANAGE';
UPDATE dbo.Permissions SET description = N'Xem dữ liệu người dùng bảo mật.' WHERE permission_code = 'SECURITY_USER_VIEW';

UPDATE dbo.Permissions SET description = N'Tạo dịch vụ theo điều kiện chuẩn.' WHERE permission_code = 'SERVICE_CREATE_STANDARD';
UPDATE dbo.Permissions SET description = N'Phê duyệt giá dịch vụ ngoại lệ.' WHERE permission_code = 'SERVICE_PRICE_OVERRIDE_APPROVE';
UPDATE dbo.Permissions SET description = N'Đề nghị giá dịch vụ ngoại lệ.' WHERE permission_code = 'SERVICE_PRICE_OVERRIDE_REQUEST';
UPDATE dbo.Permissions SET description = N'Gia hạn theo giá chuẩn đã chốt.' WHERE permission_code = 'SERVICE_RENEW_STANDARD';
UPDATE dbo.Permissions SET description = N'Quản lý danh mục loại dịch vụ.' WHERE permission_code = 'SERVICE_TYPE_MANAGE';
UPDATE dbo.Permissions SET description = N'Xem dịch vụ trong công ty được phân.' WHERE permission_code = 'SERVICE_VIEW';

UPDATE dbo.Permissions SET description = N'Quản lý thẻ (hashtag): tạo/sửa/gỡ thẻ trong danh mục và gắn/gỡ thẻ vào khách hàng, phần mộ.' WHERE permission_code = 'TAG_MANAGE';

UPDATE dbo.Permissions SET description = N'Xem nhật ký kiểm toán cấu hình và vận hành quy trình.' WHERE permission_code = 'WORKFLOW_AUDIT_VIEW';
UPDATE dbo.Permissions SET description = N'Liên kết quy trình vào nghiệp vụ.' WHERE permission_code = 'WORKFLOW_BIND_PROCESS';
UPDATE dbo.Permissions SET description = N'Quản lý cấu hình quy trình phê duyệt.' WHERE permission_code = 'WORKFLOW_CONFIG_MANAGE';
UPDATE dbo.Permissions SET description = N'Xuất bản/kích hoạt phiên bản quy trình.' WHERE permission_code = 'WORKFLOW_PUBLISH';
UPDATE dbo.Permissions SET description = N'Chuyển người duyệt cho bước đang chờ.' WHERE permission_code = 'WORKFLOW_REASSIGN_PENDING';
UPDATE dbo.Permissions SET description = N'Từ chối hồ sơ quy trình.' WHERE permission_code = 'WORKFLOW_REJECT';
UPDATE dbo.Permissions SET description = N'Chạy lại bước thực thi của quy trình.' WHERE permission_code = 'WORKFLOW_RETRY_EXECUTION';
UPDATE dbo.Permissions SET description = N'Xem quy trình phê duyệt.' WHERE permission_code = 'WORKFLOW_VIEW';
GO
