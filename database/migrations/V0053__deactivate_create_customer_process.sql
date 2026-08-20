-- V0053__deactivate_create_customer_process.sql
--
-- Vô hiệu hoá quy trình nghiệp vụ CREATE_CUSTOMER trong Business_Process_Catalog.
--
-- Bối cảnh (anh Bách 2026-08-19/20): chức năng "Đề xuất KH mới" đã bị gỡ TOÀN BỘ
-- (frontend + backend, commit 6b5b9d4) vì trùng với "Tạo khách hàng". Bộ xử lý
-- CreateCustomerExecutionHandler (ProcessCode=CREATE_CUSTOMER) đã bị xoá theo.
--
-- Hệ quả còn lại: dòng catalog CREATE_CUSTOMER vẫn is_active=1 AND is_approval_required=1
-- nên bước đối chiếu lúc khởi động (WorkflowHandlerRegistration) coi đây là "quy trình cần
-- phê duyệt nhưng thiếu bộ xử lý" và ghi 1 dòng log WRN mỗi lần khởi động. Vô hại về hành vi
-- (không còn code nào tạo được hồ sơ loại này), nhưng gây nhiễu log.
--
-- Fix sạch: set is_active=0. Bước quét chỉ lấy p.IsActive && p.IsApprovalRequired nên tắt cờ
-- này là hết cảnh báo. Không đụng is_approval_required (giữ ngữ nghĩa lịch sử), không xoá dòng
-- (giữ toàn vẹn khoá ngoại từ mọi Workflow_Definitions/hồ sơ cũ nếu có). Đảo ngược được bằng
-- cách set lại is_active=1.
--
-- Idempotent: chỉ cập nhật khi còn đang bật.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

UPDATE dbo.Business_Process_Catalog
   SET is_active = 0
 WHERE process_code = 'CREATE_CUSTOMER'
   AND is_active = 1;
