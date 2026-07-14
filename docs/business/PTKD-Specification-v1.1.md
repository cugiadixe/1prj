---
title: "PTKD Specification v1.1"
version: "1.1"
status: "approved baseline for DEV/DBA/QA review"
issued: "2026-07-14"
source_of_truth: "PTKD-Specification-v1.1.docx"
---

> Tệp Markdown này phục vụ Codex tra cứu nhanh. Khi có khác biệt về trình bày hoặc nội dung, bản `PTKD-Specification-v1.1.docx` là tài liệu phát hành chính thức. Các tệp chuyên đề trong cùng thư mục là bản trích xuất có cấu trúc, không thay thế đặc tả tổng thể.

# ĐẶC TẢ TRIỂN KHAI

## PHÂN QUYỀN, PHÊ DUYỆT VÀ WORKFLOW CẤU HÌNH ĐỘNG

**Hệ thống quản lý PTKD – INDEVCO ERP**

| **Phiên bản**             | **1.1**                                                                                |
|---------------------------|----------------------------------------------------------------------------------------|
| **Ngày ban hành dự thảo** | 14/07/2026                                                                             |
| **Trạng thái**            | Đặc tả chốt nghiệp vụ - bổ sung workflow cấu hình động, sẵn sàng cho DEV/DBA/QA review |
| **Đơn vị sử dụng**        | Phòng CNTT / Phòng PTKD / Phòng Kế toán / Nhóm quản trị dữ liệu khách hàng             |
| **Đối tượng đọc**         | DEV, BA, QA, DBA, quản trị hệ thống, Admin workflow và chủ sở hữu nghiệp vụ            |

> MỤC ĐÍCH SỬ DỤNG
> Tài liệu này là đặc tả độc lập để DEV triển khai lớp phân quyền, quản trị dữ liệu khách hàng dùng chung, phê duyệt nghiệp vụ, ủy quyền và kiểm soát hiệu chỉnh giao dịch. Mọi quy tắc trong tài liệu phải được thể hiện nhất quán tại giao diện, API/service và database đối với các nghiệp vụ quan trọng.

# KIỂM SOÁT TÀI LIỆU

| **Thuộc tính**            | **Nội dung**                                                                                                                                                                                                                           |
|---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Mục tiêu**              | Định nghĩa đầy đủ cấu trúc dữ liệu, quy tắc tính quyền, workflow phê duyệt cấu hình động, ủy quyền, quản trị khách hàng dùng chung và kiểm soát sửa payment để DEV triển khai thống nhất.                                              |
| **Phạm vi**               | Authorization, field-level permission, company scope, customer master governance, dynamic approval workflow, workflow versioning/binding, delegation, reminder, payment correction, audit, notification, migration và acceptance test. |
| **Ngoài phạm vi**         | Đăng nhập/SSO chi tiết, MFA provider, hạ tầng gửi email/SMS, chính sách lưu trữ log cấp doanh nghiệp và công cụ low-code tạo mới quy trình/biểu mẫu nghiệp vụ.                                                                         |
| **Nguồn dữ liệu chuẩn**   | Profiles và Customers dùng chung toàn Tổng công ty; dữ liệu dịch vụ, tài chính và vận hành được giới hạn theo company_id.                                                                                                              |
| **Nguyên tắc triển khai** | Admin chỉ cấu hình luồng cho quy trình đã được DEV công bố trong Business_Process_Catalog. API/service quyết định quyền; database khóa invariant, version và lịch sử workflow.                                                         |

## NỘI DUNG CHÍNH

| **Mục**     | **Tên phần**                                        |
|-------------|-----------------------------------------------------|
| **1**       | Tóm tắt quyết định nghiệp vụ                        |
| **2**       | Phạm vi dữ liệu dùng chung và dữ liệu theo công ty  |
| **3**       | Mô hình phân quyền và thuật toán tính quyền         |
| **4**       | Danh mục quyền, phòng ban, role và group Admin      |
| **5**       | Quản trị dữ liệu khách hàng dùng chung              |
| **6**       | Quyền thu tiền và hiệu chỉnh payment đã xác nhận    |
| **7**       | Kiến trúc phê duyệt và workflow cấu hình động       |
| **8**       | Ủy quyền phê duyệt có Admin kích hoạt               |
| **9**       | Đặc tả bảng, workflow definition và thay đổi schema |
| **10**      | Luồng xử lý chi tiết                                |
| **11**      | API/service và kiểm soát database                   |
| **12**      | Audit, thông báo và dữ liệu nhạy cảm                |
| **13**      | Migration và kế hoạch triển khai                    |
| **14**      | Tiêu chí nghiệm thu                                 |
| **Phụ lục** | Mã quyền, trạng thái và checklist cho DEV           |

> QUY ƯỚC
> Các từ MUST/BẮT BUỘC thể hiện yêu cầu không được bỏ qua. Các tên bảng, cột, permission code và status trong tài liệu là tên logic chuẩn; DEV chỉ được đổi sau khi BA/DBA phê duyệt thay đổi.

# 1. TÓM TẮT QUYẾT ĐỊNH NGHIỆP VỤ

Các quyết định dưới đây là baseline triển khai. Chúng không phải giả định kỹ thuật và không phụ thuộc vào việc người đọc đã biết các tài liệu trước đó.

| **Chủ đề**                 | **Quyết định bắt buộc**                                                                                                                                  |
|----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Khách hàng**             | Profiles và Customers là nguồn tài nguyên dùng chung toàn Tổng công ty; không nhân bản khách hàng theo từng công ty.                                     |
| **Sửa dữ liệu khách hàng** | Chỉ nhóm quản trị dữ liệu khách hàng được sửa toàn bộ thông tin dùng chung, bao gồm họ tên, CCCD, ngày sinh, điện thoại và địa chỉ.                      |
| **Nhân viên nghiệp vụ**    | Được tìm kiếm/sử dụng khách hàng, tạo yêu cầu thêm/sửa khách hàng và cập nhật ngữ cảnh riêng của công ty; không sửa trực tiếp dữ liệu master.            |
| **Quyền phòng ban**        | Là quyền nền thực tế. Role tại công ty cộng thêm quyền nghiệp vụ; quyền cá nhân xử lý ngoại lệ.                                                          |
| **Phạm vi dữ liệu**        | Phân quyền nghiệp vụ theo công ty, không phân quyền theo Site/Zone/Lot/Plot trong phạm vi tài liệu này.                                                  |
| **Thu tiền**               | Nhân viên có role Thu ngân được tự tạo và tự xác nhận bill; không cần người thứ hai phê duyệt payment thông thường.                                      |
| **Payment CONFIRMED**      | Chỉ group Admin có quyền PAYMENT_CORRECT_CONFIRMED được hiệu chỉnh; không cần Kế toán trưởng phê duyệt trước.                                            |
| **Thông báo sửa payment**  | Sau hiệu chỉnh phải gửi thông báo cho các bên liên quan và đánh dấu lại các kỳ đối soát bị ảnh hưởng.                                                    |
| **Gia hạn dịch vụ**        | Gia hạn đúng giá tiêu chuẩn không cần duyệt; mọi thay đổi hoặc giảm giá phải được phê duyệt.                                                             |
| **Ủy quyền duyệt**         | Người được ủy quyền không cần role Trưởng bộ phận tương đương, nhưng ủy quyền chỉ có hiệu lực sau khi Admin kích hoạt.                                   |
| **Chế độ ủy quyền**        | Là quyền bổ sung: Trưởng bộ phận giữ quyền gốc, người được ủy quyền có thêm quyền trong thời gian ACTIVE.                                                |
| **Hết hạn ủy quyền**       | Tự động thu hồi quyền bổ sung và trở về luồng gốc; không cần Admin kích hoạt lại.                                                                        |
| Cấu hình workflow          | Admin chỉ tạo luồng phê duyệt cho quy trình đã có trong Business_Process_Catalog; không tự tạo quy trình, biểu mẫu, bảng dữ liệu hoặc execution handler. |
| Kiểu duyệt                 | Chỉ duyệt tuần tự theo step_no; tại một thời điểm chỉ một bước ở trạng thái PENDING. Không hỗ trợ duyệt song song hoặc tối thiểu N người.                |
| Nguồn người duyệt          | Hỗ trợ user cụ thể, role, phòng ban, trưởng phòng, quản lý trực tiếp, permission, group Admin và user lấy từ trường dữ liệu được DEV công bố.            |
| Nhắc hạn                   | Có SLA và nhắc trước/đúng hạn/quá hạn; không tự động chuyển cấp, bỏ qua hoặc tự phê duyệt.                                                               |
| Trả lại                    | RETURN luôn quay về người gửi. Khi gửi lại tăng round_no, tạo lại step và giữ nguyên workflow version ban đầu.                                           |
| Phạm vi workflow           | GLOBAL áp dụng toàn Tổng công ty/Tập đoàn; COMPANY áp dụng riêng một công ty và được ưu tiên hơn GLOBAL.                                                 |
| Không tìm được người duyệt | Chặn gửi yêu cầu, thông báo rõ bước lỗi cho người gửi, ghi cấu hình lỗi và thông báo Admin workflow.                                                     |
| Thay đổi phiên bản         | Yêu cầu đang chạy giữ phiên bản và snapshot cũ; chỉ yêu cầu mới dùng phiên bản mới.                                                                      |

> HỆ QUẢ THIẾT KẾ CONFIRM_PAYMENT và RENEW_SERVICE đúng giá không tạo Approval_Requests. Các nghiệp vụ cần duyệt không gắn cứng người duyệt trong source code; hệ thống chọn Approval_Workflow_Bindings, snapshot workflow version và resolve toàn bộ người duyệt trước khi tạo request.

# 2. PHẠM VI DỮ LIỆU DÙNG CHUNG VÀ THEO CÔNG TY

## 2.1. Hai lớp dữ liệu

> **Sơ đồ:** Sơ đồ kiến trúc dữ liệu khách hàng dùng chung và dữ liệu nghiệp vụ theo công ty. Xem bản DOCX để xem hình minh họa gốc.

Hình 1. Dữ liệu khách hàng dùng chung và dữ liệu nghiệp vụ theo công ty

| **Nhóm**             | **Bảng/Thông tin**                            | **Phạm vi**    | **Quy tắc truy cập**                                                                              |
|----------------------|-----------------------------------------------|----------------|---------------------------------------------------------------------------------------------------|
| **Customer master**  | Profiles, Customers                           | GLOBAL         | Người có quyền xem khách hàng được tìm kiếm hồ sơ dùng chung; chỉ nhóm quản trị dữ liệu được sửa. |
| **Customer context** | Customer_Company_Context                      | COMPANY        | Chỉ người dùng có role/quyền tại company_id tương ứng.                                            |
| **Địa điểm**         | Sites → Zones → Blocks → Lots → Plots         | COMPANY        | Company xác định tại Site và được suy ra xuống cây.                                               |
| **Dịch vụ**          | Customer_Care_Services                        | COMPANY        | Lưu company_id trực tiếp vì location_id có thể NULL.                                              |
| **Tài chính**        | Payment_Transactions, Payment_Reconciliations | COMPANY        | Đã/Phải lưu company_id; lọc bắt buộc tại API.                                                     |
| **Phê duyệt**        | Approval_Requests                             | COMPANY        | Mỗi request thuộc một company_id; approver/delegate phải có quyền trong công ty đó.               |
| **Tài liệu**         | Document_Files                                | GLOBAL/COMPANY | CCCD có thể GLOBAL; hợp đồng/biên lai/payment document là COMPANY.                                |
| **Import/Export**    | Import_Logs, Export_Logs                      | COMPANY        | Lưu company_id và audit người thực hiện.                                                          |

## 2.2. Hồ sơ 360 của khách hàng

- Thông tin định danh và liên hệ lấy từ Profiles/Customers và được hiển thị dùng chung theo permission dữ liệu nhạy cảm.

- Dịch vụ, bill, đối soát, ghi chú nội bộ và tài liệu nghiệp vụ được lọc theo danh sách company_id mà user đang có quyền.

- User có role ở nhiều công ty được xem hợp nhất đúng các công ty đó; không tự động có quyền toàn Tổng công ty.

- Tổng chi tiêu theo công ty lấy từ vw_Customer_Spending_By_Company. Tổng toàn tập đoàn chỉ hiển thị khi có CUSTOMER_GROUP_FINANCE_VIEW.

> KHÔNG ĐƯỢC DÙNG
> Không dùng Customers.total_spent làm số liệu tài chính chung cho mọi user và không dùng Customers.assigned_staff_id làm nguồn nhân viên phụ trách duy nhất.

# 3. MÔ HÌNH PHÂN QUYỀN VÀ THUẬT TOÁN TÍNH QUYỀN

## 3.1. Nguồn quyền

| **Nguồn**                             | **Vai trò**                         | **Ví dụ**                                        | **Độ ưu tiên**           |
|---------------------------------------|-------------------------------------|--------------------------------------------------|--------------------------|
| **DepartmentPermissions**             | Quyền nền theo phòng ban            | PTKD được xem khách hàng, tạo dịch vụ tiêu chuẩn | Thấp                     |
| **RolePermissions**                   | Quyền bổ sung theo role tại công ty | Thu ngân xác nhận payment; Trưởng BP duyệt giá   | Trung bình               |
| **User_Individual_Permissions ALLOW** | Ngoại lệ cá nhân có thời hạn        | Tạm cấp quyền xuất báo cáo                       | Cao                      |
| **Approval_Delegations ACTIVE**       | Quyền duyệt tạm thời                | Duyệt SERVICE_PRICE_OVERRIDE thay Trưởng BP      | Chỉ trên approval step   |
| **User_Individual_Permissions DENY**  | Thu hồi/cấm quyền cá nhân           | Cấm export dù role có quyền                      | Cao nhất trong quyền mềm |
| **System hard rules**                 | Bất biến hệ thống                   | Không xóa payment CONFIRMED                      | Tuyệt đối                |

> **Sơ đồ:** Sơ đồ thứ tự đánh giá quyền hiệu lực của người dùng. Xem bản DOCX để xem hình minh họa gốc.

Hình 2. Thứ tự kiểm tra quyền tại thời điểm thực hiện hành động

## 3.2. Công thức quyền hiệu lực

> SOFT_ALLOW = DepartmentBaseAllow ∪ RoleCompanyAllow ∪ IndividualAllow(đang hiệu lực) EFFECTIVE_ALLOW = SOFT_ALLOW − IndividualDeny(đang hiệu lực) AUTHORIZED = UserActive AND HardRuleAllows(action, record_status) AND EFFECTIVE_ALLOW contains permission_code AND DataScopeAllows(user, permission.data_scope, record.company_id) Approval delegation chỉ bổ sung quyền ACT trên approval step phù hợp; không bổ sung quyền sửa entity hoặc quyền quản trị khác.

## 3.3. Quy tắc bắt buộc

- DENY cá nhân thắng mọi ALLOW từ phòng ban, role và cá nhân.

- Một user có nhiều role được hợp nhất quyền ALLOW trong cùng company_id.

- Quyền data_scope=COMPANY chỉ có hiệu lực khi tồn tại User_Role_Company ACTIVE tương ứng.

- Quyền data_scope=GLOBAL không đồng nghĩa được xem dữ liệu nhạy cảm; vẫn phải có permission thích hợp.

- Không kiểm tra quyền chỉ bằng việc ẩn/hiện nút trên UI.

- Mọi endpoint phải kiểm tra quyền lại tại server; stored procedure tài chính tiếp tục kiểm tra invariant và actor.

- UserPermissions nếu tồn tại chỉ là cache/view quyền đã tính, không phải nguồn cấp quyền gốc.

# 4. DANH MỤC QUYỀN, PHÒNG BAN, ROLE VÀ GROUP ADMIN

## 4.1. Permission catalog

Mọi nguồn quyền phải tham chiếu một permission catalog duy nhất. Không lưu các chuỗi hành động không kiểm soát riêng rẽ trong từng bảng quyền.

| **permission_code**            | **Module**     | **Action**          | **Scope**      | **Nhạy cảm** | **Delegable** |
|--------------------------------|----------------|---------------------|----------------|--------------|---------------|
| CUSTOMER_VIEW_BASIC            | CUSTOMER       | VIEW                | GLOBAL         | Không        | Không         |
| CUSTOMER_VIEW_SENSITIVE        | CUSTOMER       | VIEW_SENSITIVE      | GLOBAL         | Có           | Không         |
| CUSTOMER_CHANGE_REQUEST_CREATE | CUSTOMER       | PROPOSE_CHANGE      | GLOBAL         | Không        | Không         |
| CUSTOMER_CREATE_FINAL          | CUSTOMER       | CREATE_FINAL        | GLOBAL         | Có           | Không         |
| CUSTOMER_MASTER_UPDATE         | CUSTOMER       | UPDATE_MASTER       | GLOBAL         | Có           | Không         |
| CUSTOMER_MERGE_DUPLICATE       | CUSTOMER       | MERGE               | GLOBAL         | Có           | Không         |
| CUSTOMER_GROUP_FINANCE_VIEW    | CUSTOMER       | VIEW_GROUP_FINANCE  | GLOBAL         | Có           | Không         |
| SERVICE_CREATE_STANDARD        | SERVICE        | CREATE              | COMPANY        | Không        | Không         |
| SERVICE_RENEW_STANDARD         | SERVICE        | RENEW               | COMPANY        | Không        | Không         |
| SERVICE_PRICE_OVERRIDE_REQUEST | SERVICE        | REQUEST_PRICE       | COMPANY        | Có           | Không         |
| SERVICE_PRICE_OVERRIDE_APPROVE | SERVICE        | APPROVE_PRICE       | COMPANY        | Có           | Có            |
| PAYMENT_CREATE_DRAFT           | PAYMENT        | CREATE_DRAFT        | COMPANY        | Có           | Không         |
| PAYMENT_CONFIRM                | PAYMENT        | CONFIRM             | COMPANY        | Có           | Không         |
| PAYMENT_CORRECT_CONFIRMED      | PAYMENT        | CORRECT             | COMPANY        | Có           | Không         |
| RECONCILIATION_PREPARE         | RECONCILIATION | PREPARE             | COMPANY        | Có           | Không         |
| RECONCILIATION_CONFIRM         | RECONCILIATION | CONFIRM             | COMPANY        | Có           | Không         |
| CHANGE_OWNER_APPROVE           | PLOT           | APPROVE_OWNER       | COMPANY        | Có           | Có            |
| CARD_REPRINT_APPROVE           | CARD           | APPROVE_REPRINT     | COMPANY        | Có           | Có            |
| DELEGATION_CREATE              | APPROVAL       | DELEGATE            | COMPANY        | Có           | Không         |
| DELEGATION_ACTIVATE            | APPROVAL       | ACTIVATE_DELEGATION | COMPANY        | Có           | Không         |
| IMPORT_EXECUTE                 | IMPORT         | EXECUTE             | COMPANY        | Có           | Không         |
| IMPORT_ROLLBACK                | IMPORT         | ROLLBACK            | COMPANY        | Có           | Không         |
| SENSITIVE_EXPORT               | EXPORT         | EXPORT_SENSITIVE    | COMPANY        | Có           | Có/Policy     |
| AUDIT_VIEW                     | AUDIT          | VIEW                | COMPANY/GLOBAL | Có           | Không         |
| PAYMENT_PRINT                  | PAYMENT        | PRINT               | COMPANY        | Có           | Không         |
| WORKFLOW_VIEW                  | WORKFLOW       | VIEW                | GLOBAL/COMPANY | Không        | Không         |
| WORKFLOW_CONFIG_MANAGE         | WORKFLOW       | CONFIGURE           | GLOBAL/COMPANY | Có           | Không         |
| WORKFLOW_PUBLISH               | WORKFLOW       | PUBLISH             | GLOBAL/COMPANY | Có           | Không         |
| WORKFLOW_BIND_PROCESS          | WORKFLOW       | BIND_PROCESS        | GLOBAL/COMPANY | Có           | Không         |
| WORKFLOW_REASSIGN_PENDING      | WORKFLOW       | REASSIGN_PENDING    | COMPANY        | Có           | Không         |
| WORKFLOW_AUDIT_VIEW            | WORKFLOW       | VIEW_AUDIT          | GLOBAL/COMPANY | Có           | Không         |

## 4.2. Quyền nền phòng ban

| **Phòng ban**       | **Quyền nền đề xuất**                                                                                  | **Không cấp ở quyền nền**                                            |
|---------------------|--------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------|
| **PTKD/Kinh doanh** | Xem khách hàng; tạo yêu cầu customer; xem phần mộ; tạo/gia hạn dịch vụ đúng giá; xem bill theo công ty | Xác nhận tiền; duyệt giá; sửa customer master; sửa payment CONFIRMED |
| **Kế toán**         | Xem payment; xem báo cáo; xem kỳ đối soát theo công ty                                                 | Sửa payment; quản lý user; sửa customer master                       |
| **CNTT**            | Xem tình trạng kỹ thuật và cấu hình được giao                                                          | Không mặc nhiên xem dữ liệu nhạy cảm hoặc sửa tài chính              |

## 4.3. Role nghiệp vụ

| **Role**                      | **Quyền bổ sung**                                                          | **Phạm vi**                                          |
|-------------------------------|----------------------------------------------------------------------------|------------------------------------------------------|
| **CASHIER**                   | PAYMENT_CREATE_DRAFT, PAYMENT_CONFIRM, PAYMENT_PRINT                       | Theo company_id                                      |
| **PTKD_MANAGER**              | SERVICE_PRICE_OVERRIDE_APPROVE, CHANGE_OWNER_APPROVE, CARD_REPRINT_APPROVE | Theo company_id; các quyền duyệt cho phép delegation |
| **ACCOUNTANT_RECONCILER**     | RECONCILIATION_CONFIRM, báo cáo và xuất tài chính được cấp                 | Theo company_id                                      |
| **GROUP_CUSTOMER_DATA_ADMIN** | CUSTOMER_CREATE_FINAL, CUSTOMER_MASTER_UPDATE, CUSTOMER_MERGE_DUPLICATE    | GLOBAL toàn Tổng công ty                             |
| **AUDITOR**                   | AUDIT_VIEW và quyền đọc cần thiết                                          | Theo scope được gán; không sửa                       |

## 4.4. Group quyền Admin

| **Role/Group**           | **Phạm vi quyền**                                                                                                                  |
|--------------------------|------------------------------------------------------------------------------------------------------------------------------------|
| **ADMIN_SECURITY**       | User, role, permission, khóa/mở tài khoản, kích hoạt ủy quyền                                                                      |
| **ADMIN_CUSTOMER_DATA**  | Quản trị customer master và xử lý trùng                                                                                            |
| **ADMIN_LOCATION_DATA**  | Site, Zone, Block, Lot, Plot                                                                                                       |
| **ADMIN_SERVICE_DATA**   | Danh mục/gói dịch vụ và dữ liệu dịch vụ                                                                                            |
| **ADMIN_PAYMENT**        | Hiệu chỉnh payment CONFIRMED                                                                                                       |
| **ADMIN_RECONCILIATION** | Quản trị kỳ đối soát                                                                                                               |
| **ADMIN_IMPORT**         | Import, rollback, xử lý xung đột                                                                                                   |
| **ADMIN_DOCUMENT**       | Tài liệu, quarantine, versioning                                                                                                   |
| **ADMIN_AUDIT**          | Danh mục và cấu hình hệ thống; quản trị workflow DRAFT. Publish/binding yêu cầu WORKFLOW_PUBLISH/WORKFLOW_BIND_PROCESS theo scope. |
| **ADMIN_SYSTEM_CONFIG**  | Danh mục và cấu hình hệ thống                                                                                                      |
| **SUPER_ADMIN**          | Tập hợp toàn bộ group Admin; vẫn chịu các invariant cứng và audit                                                                  |

> NGUYÊN TẮC ADMIN
> Admin có toàn bộ quyền nghiệp vụ theo group được cấp, nhưng không được xóa dấu vết, bỏ audit hoặc phá vỡ các bất biến như bill_code, trạng thái CONFIRMED và currency_code=VND.

# 5. QUẢN TRỊ DỮ LIỆU KHÁCH HÀNG DÙNG CHUNG

## 5.1. Phân loại trường và quyền sửa

| **Nhóm**             | **Trường tiêu biểu**                                              | **Ai được sửa**                                  | **Kiểm soát**                    |
|----------------------|-------------------------------------------------------------------|--------------------------------------------------|----------------------------------|
| **Định danh**        | Profiles.full_name, cccd, dob, dob_partial, dob_precision, gender | GROUP_CUSTOMER_DATA_ADMIN                        | Bắt buộc reason + before/after   |
| **Pháp lý**          | permanent_address, cccd_issue_date, cccd_issue_place, tax_code    | GROUP_CUSTOMER_DATA_ADMIN                        | Dữ liệu nhạy cảm                 |
| **Liên hệ**          | phone, contact_address                                            | GROUP_CUSTOMER_DATA_ADMIN                        | Nhân viên thường chỉ gửi đề nghị |
| **Người mất**        | death_date_solar/lunar, death_place, hometown                     | GROUP_CUSTOMER_DATA_ADMIN hoặc role chuyên trách | Ghi audit đầy đủ                 |
| **Ngữ cảnh công ty** | assigned_staff_id, internal_notes, relationship_status            | Người có quyền tại company_id                    | Lưu Customer_Company_Context     |
| **Trường hệ thống**  | id, created_at/by, row_version                                    | Không sửa trực tiếp                              | Do hệ thống quản lý              |

## 5.2. Luồng nhân viên đề nghị – nhóm quản trị tạo/sửa chính thức

1.  Nhân viên tìm kiếm customer master bằng CCCD, họ tên, ngày sinh và điện thoại trước khi tạo yêu cầu.

2.  Nếu chưa tồn tại, nhân viên gửi CREATE_CUSTOMER; nếu cần sửa, gửi CUSTOMER_MASTER_CHANGE.

3.  Approval_Requests lưu after_data; yêu cầu sửa lưu thêm before_data và target_version.

4.  Nhóm quản trị dữ liệu kiểm tra trùng, chuẩn hóa và xử lý yêu cầu.

5.  Khi được duyệt, hệ thống tạo/cập nhật Profiles + Customers trong một transaction và ghi Approval_Actions/System_Audit_Logs.

6.  Hệ thống tạo hoặc cập nhật Customer_Company_Context cho công ty gửi yêu cầu.

7.  Nhân viên gửi yêu cầu được thông báo kết quả; tài liệu đính kèm được chuyển từ entity APPROVAL sang CUSTOMER khi thực thi thành công.

> NHÓM QUẢN TRỊ TỰ PHÁT HIỆN SAI
> Nhóm quản trị dữ liệu được sửa trực tiếp customer master nếu tự phát hiện lỗi. Đây không phải tự duyệt Approval_Requests, nhưng vẫn bắt buộc nhập lý do và ghi field-level before/after.

## 5.3. Chống trùng và gộp khách hàng

- CCCD tạo unique filtered index khi có dữ liệu và bản ghi đang hoạt động.

- Điện thoại không unique tuyệt đối; chỉ dùng làm tín hiệu phát hiện trùng.

- CREATE_CUSTOMER phải chạy duplicate check trước khi gửi và trước khi thực thi.

- CUSTOMER_MERGE_DUPLICATE chỉ do GROUP_CUSTOMER_DATA_ADMIN thực hiện; phải preview toàn bộ service, payment, document và company context bị ảnh hưởng.

- Không xóa lịch sử customer nguồn sau merge; đánh dấu MERGED và lưu survivor_customer_id trong audit/mapping migration thích hợp.

# 6. QUYỀN THU TIỀN VÀ HIỆU CHỈNH PAYMENT ĐÃ XÁC NHẬN

## 6.1. Thu ngân tự tạo và tự xác nhận

Một user có role CASHIER và permission PAYMENT_CONFIRM được phép vừa tạo vừa xác nhận payment. Hệ thống không tạo Approval_Requests cho CONFIRM_PAYMENT.

1.  Tạo Payment_Transactions ở trạng thái DRAFT và sinh bill_code duy nhất.

2.  Thêm tối thiểu một Payment_Transaction_Items; kiểm tra customer/company/service cycle.

3.  Tính total_amount từ SUM(item_amount); không tin giá trị tổng do client gửi.

4.  Thu ngân nhập payment_method, payment_date, payer và xác nhận.

5.  Khi xác nhận: received_by và confirmed_by có thể cùng là user hiện tại; status chuyển một chiều sang CONFIRMED.

6.  Sau xác nhận, thu ngân chỉ được xem/in; không được sửa hoặc xóa.

> KIỂM SOÁT BÙ
> Do không áp dụng maker-checker tại từng bill, đối soát ngày là bắt buộc; quyền PAYMENT_CONFIRM chỉ cấp cho role Thu ngân; payment CONFIRMED bị khóa và mọi hiệu chỉnh Admin đều có thông báo sau sửa.

## 6.2. Phạm vi sửa của ADMIN_PAYMENT

| **Nhóm trường**        | **Được sửa**                                                   | **Không được sửa**                                             |
|------------------------|----------------------------------------------------------------|----------------------------------------------------------------|
| **Định danh hệ thống** | —                                                              | id, bill_code, created_at/by                                   |
| **Trạng thái/tiền tệ** | —                                                              | status phải giữ CONFIRMED; currency_code phải giữ VND          |
| **Phạm vi**            | company_id                                                     | Không được để dữ liệu liên quan lệch company                   |
| **Khách hàng/dịch vụ** | customer_id; thêm/sửa/xóa item; care_service_id; cycle         | Không được vi phạm unique chu kỳ hoặc quan hệ customer/company |
| **Số tiền**            | item_amount, total_amount                                      | total_amount phải bằng tổng item và \>0                        |
| **Thu tiền**           | payment_date, payment_method, payer, received_by, confirmed_by | Không tạo trạng thái cancel/refund/partial                     |
| **Ghi chú**            | notes và correction_reason                                     | correction_reason không được để trống                          |

## 6.3. Xử lý dây chuyền khi sửa

| **Thay đổi**                 | **Xử lý bắt buộc trong cùng transaction**                                                                   |
|------------------------------|-------------------------------------------------------------------------------------------------------------|
| **company_id**               | Kiểm tra item/service thuộc công ty mới; ensure Customer_Company_Context; đánh dấu kỳ ngày/tháng cũ và mới. |
| **customer_id**              | Kiểm tra toàn bộ item thuộc customer mới; cập nhật view/cache khách hàng cũ và mới; ensure company context. |
| **service/item**             | Kiểm tra customer/company/cycle; không để một cycle được thanh toán hai lần.                                |
| **item_amount/total**        | Tính lại total từ item; cập nhật doanh thu và reconciliation aggregates.                                    |
| **payment_date**             | Đánh dấu kỳ ngày và tháng cũ/mới là DIFFERENCE nếu đã CONFIRMED.                                            |
| **payment_method**           | Tính lại cơ cấu CASH/BANK_TRANSFER/CARD của kỳ.                                                             |
| **received_by/confirmed_by** | Kiểm tra user còn hiệu lực và có quan hệ với company tương ứng.                                             |

Nếu đồng thời đổi công ty và ngày thanh toán, có thể ảnh hưởng tối đa bốn kỳ: ngày cũ, tháng cũ, ngày mới và tháng mới. Stored procedure phải thu thập đầy đủ danh sách kỳ trước khi cập nhật.

## 6.4. Thông báo sau hiệu chỉnh

- Gửi cho người thu/xác nhận bill, Trưởng PTKD và nhóm Kế toán đối soát của công ty liên quan.

- Nếu đổi company_id, gửi cho cả công ty cũ và công ty mới.

- Thông báo chứa bill_code, Admin thực hiện, lý do, danh sách trường đổi, giá trị quan trọng trước/sau và các kỳ đối soát bị ảnh hưởng.

- Thông báo liên kết đến audit detail; không gửi link file/public URL vĩnh viễn.

# 7. KIẾN TRÚC PHÊ DUYỆT VÀ WORKFLOW CẤU HÌNH ĐỘNG

## 7.1. Danh mục quy trình hỗ trợ phê duyệt

| **process_code**         | **Người gửi** | **Workflow/approver resolution**                      | **Kết quả thực thi**                          |
|--------------------------|---------------|-------------------------------------------------------|-----------------------------------------------|
| CREATE_CUSTOMER          | Nhân viên     | Binding động; rule mặc định GROUP_CUSTOMER_DATA_ADMIN | Tạo Profiles + Customers + company context    |
| CUSTOMER_MASTER_CHANGE   | Nhân viên     | Binding động; rule mặc định GROUP_CUSTOMER_DATA_ADMIN | Cập nhật customer master sau kiểm tra version |
| CUSTOMER_MERGE_DUPLICATE | Nhóm quản trị | Binding động theo policy quản trị dữ liệu             | Gộp và giữ mapping/audit                      |
| CHANGE_OWNER             | PTKD          | Binding động; thường dùng PTKD_MANAGER/permission     | Cập nhật owner + history                      |
| SERVICE_PRICE_OVERRIDE   | PTKD          | Binding động; thường dùng PTKD_MANAGER/permission     | Áp dụng giá khác standard snapshot            |
| CARD_REPRINT             | PTKD          | Binding động; ví dụ Trưởng PTKD -\> Kế toán           | Cho phép in lại và ghi log                    |
| IMPORT_ROLLBACK          | Admin Import  | Binding động theo scope quản trị import               | Rollback có version check                     |
| SENSITIVE_EXPORT         | User có quyền | Binding động theo policy dữ liệu nhạy cảm             | Cho phép export có log                        |

> KHÔNG TẠO APPROVAL
> CONFIRM_PAYMENT và gia hạn dịch vụ đúng standard_price_snapshot không tạo Approval_Requests.

## 7.2. Trạng thái request và execution

| **Nhóm**           | **Giá trị**  | **Ý nghĩa**                                                      |
|--------------------|--------------|------------------------------------------------------------------|
| **Request status** | PENDING      | Đã gửi, chờ người duyệt                                          |
| **Request status** | IN_REVIEW    | Đã được tiếp nhận                                                |
| **Request status** | RETURNED     | Trả lại để bổ sung; không phải REJECTED                          |
| **Request status** | APPROVED     | Đã hoàn tất tất cả bước duyệt                                    |
| **Request status** | REJECTED     | Từ chối, không áp dụng dữ liệu                                   |
| **Request status** | WITHDRAWN    | Người gửi rút trước khi có quyết định cuối                       |
| **Request status** | EXPIRED      | Hết hạn xử lý                                                    |
| **Execution**      | NOT_EXECUTED | Chưa áp dụng                                                     |
| **Execution**      | EXECUTING    | Đang thực thi idempotent                                         |
| **Execution**      | EXECUTED     | Đã áp dụng thành công                                            |
| **Execution**      | FAILED       | Duyệt xong nhưng thực thi lỗi; cần retry kiểm soát               |
| Step status        | WAITING      | Bước tương lai, chưa đến lượt xử lý.                             |
| Step status        | PENDING      | Bước duy nhất đang chờ người có quyền xử lý.                     |
| Step status        | CANCELLED    | Bước chưa xử lý bị đóng khi request RETURNED/REJECTED/WITHDRAWN. |

## 7.3. Nguyên tắc maker–approver và concurrency

- requested_by không được là acted_by tại bất kỳ approval step nào của chính request đó, kể cả khi có delegation.

- Khi target_version thay đổi, request không được ghi đè; chuyển RETURNED/CONFLICT theo policy.

- Hai approver hợp lệ có thể cùng nhìn thấy một step, nhưng chỉ hành động đầu tiên commit thành công; hành động sau nhận HTTP 409/CONFLICT.

- Approval và execution là hai trạng thái riêng. Execution retry chỉ dùng payload đã được duyệt và cùng payload_hash/correlation_id.

- before_data/after_data là snapshot phục vụ duyệt và audit, không thay thế bảng nghiệp vụ nguồn.

## 7.4. Phân tách design-time và runtime

Workflow được tách thành hai lớp. Lớp design-time lưu quy trình được phép cấu hình, workflow, version, step, approver rule, condition, reminder policy và binding. Lớp runtime lưu request, snapshot, step đã resolve, assignee, action và reminder log. Không dùng bảng runtime làm cấu hình nguồn.

| **Lớp**     | **Đối tượng chính**                                                                        | **Quy tắc**                                                      |
|-------------|--------------------------------------------------------------------------------------------|------------------------------------------------------------------|
| Design-time | Business_Process_Catalog, Approval_Workflows, Versions, Steps, Rules, Conditions, Bindings | Chỉ DRAFT được sửa; PUBLISHED/ACTIVE bất biến và có audit.       |
| Runtime     | Approval_Requests, Request_Steps, Step_Assignees, Actions, Reminder_Logs                   | Snapshot version khi submit; không bị thay đổi bởi cấu hình mới. |

## 7.5. Phạm vi Admin được cấu hình

- Admin được tạo workflow mới, tạo version DRAFT, thêm/xóa/sắp xếp bước, chọn approver rule, điều kiện, SLA, lịch nhắc và gán workflow vào process_code đã có.

- Admin không được tạo process_code, biểu mẫu, bảng dữ liệu, field condition mới, resolver mới hoặc execution handler mới. Các nội dung này phải do DEV phát hành.

- Admin không được nhập SQL, JavaScript hoặc biểu thức tự do. Condition chỉ được chọn từ field/operator/value catalog do DEV công bố.

- Phiên bản đã PUBLISHED/ACTIVE không sửa trực tiếp; mọi thay đổi phải clone thành version mới.

## 7.6. Phạm vi GLOBAL/COMPANY và thứ tự chọn binding

1. Khi submit, hệ thống tìm binding COMPANY đang hiệu lực của company_id và process_code.

2. Nếu không có binding COMPANY phù hợp, hệ thống tìm binding GLOBAL trong cùng tenant/Tổng công ty.

3. Nếu nhiều binding cùng phù hợp, chọn priority nhỏ nhất; nếu vẫn trùng thì chặn publish cấu hình, không lựa chọn ngẫu nhiên.

4. Nếu process bắt buộc duyệt nhưng không có binding hiệu lực, chặn submit và thông báo người gửi/Admin workflow.

| **scope_type** | **company_id** | **Độ ưu tiên** | **Ý nghĩa**                                                |
|----------------|----------------|----------------|------------------------------------------------------------|
| COMPANY        | Bắt buộc       | 1              | Áp dụng riêng công ty, ghi đè GLOBAL.                      |
| GLOBAL         | NULL           | 2              | Áp dụng toàn Tổng công ty/Tập đoàn theo tenant triển khai. |

## 7.7. Luồng duyệt tuần tự

- Workflow chỉ hỗ trợ SEQUENTIAL. Step đầu tiên là PENDING; các step sau là WAITING.

- Khi step hiện tại APPROVED, hệ thống kích hoạt đúng step_no kế tiếp trong cùng transaction.

- Một step có thể có nhiều assignee đủ điều kiện nhưng chỉ cần một người hành động; transaction đầu tiên thành công sẽ đóng step.

- Không hỗ trợ duyệt song song, tất cả cùng duyệt hoặc tối thiểu N người trong phiên bản 1.1.

## 7.8. Quy tắc xác định người duyệt

| **approver_rule_type** | **Cách resolve**                           | **Dữ liệu cấu hình tối thiểu**                 |
|------------------------|--------------------------------------------|------------------------------------------------|
| SPECIFIC_USER          | Một user cụ thể                            | user_id                                        |
| ROLE                   | User ACTIVE có role tại scope của request  | role_code, optional company scope              |
| DEPARTMENT             | User ACTIVE thuộc phòng ban                | department_id; có thể tạo nhiều assignee       |
| DEPARTMENT_MANAGER     | Trưởng phòng của phòng ban cấu hình        | department_id hoặc source=requester.department |
| REQUESTER_MANAGER      | Quản lý trực tiếp của người gửi            | Quan hệ quản lý đang hiệu lực                  |
| PERMISSION             | User có permission hiệu lực tại company    | permission_code                                |
| ADMIN_GROUP            | Thành viên group Admin theo scope          | admin_group_code                               |
| DATA_FIELD_USER        | User lấy từ field nghiệp vụ được whitelist | field_code, ví dụ assigned_staff_id            |

- Tại submit, hệ thống resolve toàn bộ step và lưu Approval_Request_Step_Assignees; không chờ đến step sau mới resolve.

- Loại ROLE/DEPARTMENT/PERMISSION/ADMIN_GROUP có thể sinh nhiều assignee, nhưng vẫn là một bước tuần tự và chỉ một action được commit.

- requested_by không được là assignee hợp lệ của chính request. Nếu loại bỏ người gửi làm step không còn assignee thì submit bị chặn.

## 7.9. RETURN và RESUBMIT

1. RETURN chuyển request sang RETURNED, step hiện tại sang RETURNED và các step WAITING phía sau sang CANCELLED.

2. Người gửi nhận lý do, chỉnh sửa payload và resubmit; hệ thống tăng round_no.

3. Vòng mới tạo lại step/assignee, chạy lại kiểm tra quyền và người duyệt nhưng tiếp tục dùng workflow_version_id ban đầu.

4. Muốn dùng workflow version mới, người gửi phải WITHDRAW request cũ và tạo request nghiệp vụ mới.

## 7.10. Versioning và tính bất biến

| **Trạng thái version** | **Cho phép**                                                  | **Không cho phép**                         |
|------------------------|---------------------------------------------------------------|--------------------------------------------|
| DRAFT                  | Sửa step/rule/condition/reminder, validate, xóa nếu chưa dùng | Không nhận request                         |
| PUBLISHED              | Đặt effective_from, chuẩn bị kích hoạt                        | Sửa cấu trúc hoặc xóa                      |
| ACTIVE                 | Nhận request mới theo binding                                 | Sửa cấu trúc hoặc thay snapshot request cũ |
| RETIRED                | Chỉ phục vụ lịch sử/request cũ                                | Nhận request mới                           |

- Approval_Requests lưu workflow_id, workflow_version_id, workflow_binding_id, workflow_snapshot và workflow_snapshot_hash.

- Yêu cầu đang chạy giữ nguyên version/snapshot cũ. Version mới chỉ áp dụng cho request submit sau effective_from.

- Không hỗ trợ tự động migrate request đang chạy sang version mới trong phiên bản 1.1.

## 7.11. Chặn submit khi không resolve được người duyệt

- Trước khi tạo request chính thức, hệ thống phải validate workflow, binding, condition, step order và resolve ít nhất một assignee hợp lệ cho mọi step.

- Nếu bất kỳ step nào không có assignee, không tạo request/step; trả lỗi APPROVAL_APPROVER_NOT_RESOLVED và nêu step_name/step_no.

- Thông báo được gửi cho người gửi và Admin workflow theo scope; log cấu hình lỗi không chứa dữ liệu nhạy cảm ngoài mức cần thiết.

## 7.12. SLA và nhắc hạn

- Mỗi step có thể cấu hình due_duration, reminder_before, reminder_at_due và reminder_repeat_after_overdue.

- Khi quá hạn, step vẫn PENDING và đánh dấu is_overdue=1; không tự chuyển cấp, không tự APPROVE/REJECT và không bỏ qua.

- Reminder job idempotent, ghi Approval_Reminder_Logs để chống gửi trùng và lưu lỗi gửi.

# 8. ỦY QUYỀN PHÊ DUYỆT CÓ ADMIN KÍCH HOẠT

> **Sơ đồ:** Sơ đồ vòng đời ủy quyền phê duyệt theo chế độ quyền bổ sung. Xem bản DOCX để xem hình minh họa gốc.

Hình 3. Vòng đời ủy quyền và quyền duyệt bổ sung

## 8.1. Quy trình

1.  Trưởng bộ phận có permission gốc tạo delegation, chọn người nhận, company, approval scope và thời hạn.

2.  Người nhận chấp nhận trách nhiệm; status chuyển PENDING_ADMIN.

3.  Admin có DELEGATION_ACTIVATE kiểm tra và kích hoạt. Nếu effective_from ở tương lai, status=SCHEDULED; nếu đã đến, status=ACTIVE.

4.  Trong ACTIVE, Trưởng bộ phận và người nhận cùng nhìn thấy các step phù hợp. Một người xử lý trước sẽ đóng step.

5.  Đến effective_to, job hệ thống chuyển EXPIRED và thu hồi quyền bổ sung; không cần Admin thao tác.

6.  Delegator hoặc Admin có thể thu hồi trước hạn; việc thu hồi có hiệu lực ngay vì làm giảm quyền.

## 8.2. Quy tắc bắt buộc

- Người được ủy quyền không cần role Trưởng bộ phận tương đương.

- Delegation chỉ cấp đúng approval permission có is_delegable=1; không cấp quyền sửa dữ liệu hoặc các quyền khác của delegator.

- Không cho người nhận duyệt request do chính mình tạo.

- Không cho ủy quyền tiếp (no delegation chaining).

- Không có hai delegation ACTIVE/SCHEDULED trùng delegator + company + approval_scope + khoảng thời gian, trừ khi policy cho phép rõ ràng.

- Lưu acted_by, on_behalf_of và delegation_id cho mọi hành động thay mặt.

- Khi hết hạn, request chưa xử lý tiếp tục nằm trong queue của primary approver; lịch sử đã xử lý không thay đổi.

# 9. ĐẶC TẢ BẢNG, WORKFLOW DEFINITION VÀ THAY ĐỔI SCHEMA

## 9.1. Tổng quan thay đổi

| **Loại** | **Bảng**                              | **Mục đích**                                                              |
|----------|---------------------------------------|---------------------------------------------------------------------------|
| NEW      | Permissions                           | Danh mục permission duy nhất                                              |
| NEW      | Customer_Company_Context              | Ngữ cảnh khách hàng riêng theo công ty                                    |
| NEW      | Approval_Request_Steps                | Các bước duyệt và người thực tế hành động                                 |
| NEW      | Approval_Actions                      | Event log bất biến của workflow                                           |
| NEW      | Approval_Delegations                  | Ủy quyền có chấp nhận, Admin kích hoạt và tự hết hạn                      |
| ALTER    | Approval_Requests                     | Bổ sung company, request_code, round, payload hash và trạng thái thực thi |
| ALTER    | User_Role_Company                     | Hiệu lực theo thời gian và scope công ty/Tổng công ty                     |
| ALTER    | DepartmentPermissions/RolePermissions | Tham chiếu Permissions.permission_code                                    |
| ALTER    | User_Individual_Permissions           | ALLOW/DENY, reason, thời hạn, người cấp                                   |
| ALTER    | Customer_Staff_History                | Liên kết customer context/company                                         |
| ALTER    | Customer_Care_Services                | company_id và dữ liệu giá chuẩn/ngoại lệ                                  |
| ALTER    | Document_Files                        | data_scope và company_id                                                  |
| ALTER    | Import_Logs                           | company_id                                                                |
| NEW      | Business_Process_Catalog              | Danh mục quy trình do DEV công bố cho Admin gán workflow                  |
| NEW      | Approval_Workflows                    | Danh tính workflow ổn định theo process/module                            |
| NEW      | Approval_Workflow_Versions            | Phiên bản DRAFT/PUBLISHED/ACTIVE/RETIRED                                  |
| NEW      | Approval_Workflow_Steps               | Bước duyệt tuần tự và SLA                                                 |
| NEW      | Approval_Step_Approver_Rules          | Quy tắc resolve người duyệt                                               |
| NEW      | Approval_Step_Conditions              | Điều kiện áp dụng step/binding từ field whitelist                         |
| NEW      | Approval_Workflow_Bindings            | Gán workflow version vào process theo GLOBAL/COMPANY                      |
| NEW      | Approval_Reminder_Policies            | Chính sách nhắc hạn của step                                              |
| NEW      | Approval_Request_Step_Assignees       | Assignee runtime đã resolve                                               |
| NEW      | Approval_Reminder_Logs                | Log gửi nhắc idempotent                                                   |

> SỐ LƯỢNG BẢNG Nếu baseline schema có 51 bảng permanent, v1.1 bổ sung 5 bảng và v1.1 bổ sung thêm 10 bảng workflow/configuration/runtime, tổng dự kiến là 66 bảng permanent. Con số cuối phải đối chiếu schema thực tế trước migration.

## 9.2. Permissions

| **Trường**      | **Kiểu**           | **Null** | **Ràng buộc**  | **Mô tả**               |
|-----------------|--------------------|----------|----------------|-------------------------|
| permission_code | varchar(100)       | NO       | PK             | Mã quyền bất biến       |
| module_code     | varchar(50)        | NO       | INDEX          | CUSTOMER/PAYMENT/...    |
| action_code     | varchar(50)        | NO       |                | VIEW/CREATE/APPROVE/... |
| data_scope      | varchar(20)        | NO       | GLOBAL/COMPANY | Phạm vi dữ liệu         |
| is_sensitive    | bit                | NO       | 0              | Quyền nhạy cảm          |
| is_delegable    | bit                | NO       | 0              | Cho phép delegation     |
| requires_reason | bit                | NO       | 0              | Bắt buộc reason         |
| description     | nvarchar(500)      | NO       |                | Mô tả cho Admin/DEV     |
| is_active       | bit                | NO       | 1              | Hiệu lực                |
| created/updated | audit + rowversion | NO/YES   | Chuẩn audit    | Quản trị catalog        |

## 9.3. Customer_Company_Context

| **Trường**           | **Kiểu**           | **Null** | **Ràng buộc** | **Mô tả**                  |
|----------------------|--------------------|----------|---------------|----------------------------|
| id                   | bigint IDENTITY    | NO       | PK            | Khóa kỹ thuật              |
| customer_id          | nvarchar(50)       | NO       | FK Customers  | Customer master dùng chung |
| company_id           | theo Companies.id  | NO       | FK Companies  | Công ty có quan hệ         |
| assigned_staff_id    | nvarchar(50)       | YES      | FK Users      | NV phụ trách tại công ty   |
| relationship_status  | varchar(20)        | NO       | ACTIVE        | ACTIVE/INACTIVE            |
| internal_notes       | nvarchar(2000)     | YES      |               | Ghi chú riêng công ty      |
| first_interaction_at | datetime2(3)       | YES      |               | Lần đầu giao dịch          |
| last_interaction_at  | datetime2(3)       | YES      |               | Lần gần nhất               |
| created/updated      | audit + rowversion | NO/YES   |               | Audit                      |

> CONSTRAINT
> UNIQUE(customer_id, company_id). Bảng không nhân bản khách hàng; chỉ lưu thông tin quan hệ riêng theo công ty.

## 9.4. Approval_Request_Steps

| **Trường**          | **Kiểu**        | **Null** | **Ràng buộc**           | **Mô tả**                                            |
|---------------------|-----------------|----------|-------------------------|------------------------------------------------------|
| id                  | bigint IDENTITY | NO       | PK                      | Khóa step                                            |
| request_id          | bigint          | NO       | FK Approval_Requests    | Yêu cầu cha                                          |
| round_no            | smallint        | NO       | 1                       | Lần gửi/lần gửi lại                                  |
| step_no             | smallint        | NO       |                         | Thứ tự bước                                          |
| required_permission | varchar(100)    | NO       | FK Permissions          | Quyền để xử lý                                       |
| primary_approver_id | nvarchar(50)    | YES      | FK Users                | Người duyệt chính đã resolve                         |
| status              | varchar(20)     | NO       | PENDING                 | PENDING/APPROVED/REJECTED/RETURNED/EXPIRED           |
| assigned_at/due_at  | datetime2(3)    | NO/YES   |                         | SLA                                                  |
| acted_by            | nvarchar(50)    | YES      | FK Users                | Người thực tế xử lý                                  |
| on_behalf_of        | nvarchar(50)    | YES      | FK Users                | Delegator nếu xử lý thay                             |
| delegation_id       | bigint          | YES      | FK Approval_Delegations | Căn cứ quyền tạm thời                                |
| action_note         | nvarchar(1000)  | YES      |                         | Ý kiến                                               |
| acted_at            | datetime2(3)    | YES      |                         | Thời điểm                                            |
| row_version         | rowversion      | NO       |                         | Chống duyệt hai lần                                  |
| workflow_step_id    | bigint          | YES      | FK Workflow_Steps       | Step nguồn đã snapshot                               |
| step_name           | nvarchar(200)   | NO       |                         | Tên snapshot để hiển thị/audit                       |
| status              | varchar(20)     | NO       | CHECK                   | WAITING/PENDING/APPROVED/REJECTED/RETURNED/CANCELLED |
| is_overdue          | bit             | NO       | 0                       | Đánh dấu quá hạn; không đổi người duyệt              |
| activated_at        | datetime2(3)    | YES      |                         | Thời điểm WAITING -\> PENDING                        |

> UNIQUE
> UNIQUE(request_id, round_no, step_no). requested_by không được là acted_by. Một step chỉ được chuyển khỏi PENDING đúng một lần trong transaction.

## 9.5. Approval_Actions

| **Trường**            | **Kiểu**         | **Null** | **Ràng buộc**        | **Mô tả**                                 |
|-----------------------|------------------|----------|----------------------|-------------------------------------------|
| id                    | bigint IDENTITY  | NO       | PK                   | Event id                                  |
| request_id            | bigint           | NO       | FK Approval_Requests | Yêu cầu                                   |
| step_id               | bigint           | YES      | FK Steps             | Bước liên quan                            |
| action                | varchar(30)      | NO       | CHECK                | SUBMIT/APPROVE/REJECT/RETURN/RESUBMIT/... |
| actor_id              | nvarchar(50)     | NO       | FK Users             | Người thực hiện                           |
| on_behalf_of          | nvarchar(50)     | YES      | FK Users             | Người được đại diện                       |
| delegation_id         | bigint           | YES      | FK Delegations       | Ủy quyền                                  |
| from_status/to_status | varchar(20)      | YES      |                      | Chuyển trạng thái                         |
| action_note           | nvarchar(1000)   | YES      |                      | Ý kiến                                    |
| action_data           | nvarchar(MAX)    | YES      | ISJSON               | Metadata/snapshot chọn lọc                |
| correlation_id        | uniqueidentifier | NO       | INDEX                | Truy vết                                  |
| created_at            | datetime2(3)     | NO       | sysdatetime()        | Bất biến                                  |

> BẤT BIẾN
> Approval_Actions không UPDATE/DELETE. Mọi sửa sai được ghi bằng action mới.

## 9.6. Approval_Delegations

| **Trường**              | **Kiểu**               | **Null** | **Ràng buộc**    | **Mô tả**                                             |
|-------------------------|------------------------|----------|------------------|-------------------------------------------------------|
| id / delegation_code    | bigint / nvarchar(30)  | NO       | PK / UK          | Khóa và mã hiển thị                                   |
| company_id              | theo Companies.id      | NO       | FK               | Công ty áp dụng                                       |
| delegator_id            | nvarchar(50)           | NO       | FK Users         | Người ủy quyền                                        |
| delegate_id             | nvarchar(50)           | NO       | FK Users         | Người nhận                                            |
| approval_permission     | varchar(100)           | NO       | FK Permissions   | Phải is_delegable=1                                   |
| effective_from/to       | datetime2(3)           | NO       | from \< to       | Thời gian hiệu lực                                    |
| reason                  | nvarchar(500)          | NO       |                  | Lý do                                                 |
| status                  | varchar(30)            | NO       | CHECK            | PENDING_ACCEPTANCE/PENDING_ADMIN/SCHEDULED/ACTIVE/... |
| accepted_by/at          | nvarchar(50)/datetime2 | YES      |                  | Người nhận chấp nhận                                  |
| activated_by/at         | nvarchar(50)/datetime2 | YES      | Admin permission | Admin kích hoạt                                       |
| rejected/revoked fields | actor/time/reason      | YES      |                  | Từ chối/thu hồi                                       |
| created/updated         | audit + rowversion     | NO/YES   |                  | Audit                                                 |

## 9.7. Approval_Requests – trường cần bổ sung/chuẩn hóa

| **Trường**                           | **Yêu cầu**                                                        |
|--------------------------------------|--------------------------------------------------------------------|
| **request_code**                     | Unique, mã hiển thị APR-YYYYMMDD-XXXXX                             |
| **company_id**                       | Bắt buộc cho các request nghiệp vụ theo công ty                    |
| **request_type**                     | Theo catalog tại mục 7.1                                           |
| **target_entity/target_id**          | target_id có thể NULL cho CREATE_CUSTOMER                          |
| **target_version**                   | rowversion khi gửi; kiểm tra lại trước execution                   |
| **before_data/after_data**           | JSON hợp lệ; snapshot chọn lọc; bảo vệ PII                         |
| **payload_hash**                     | SHA-256 của payload được duyệt; chống thay đổi khi retry           |
| **current_round_no/current_step_no** | Theo dõi resubmit và step hiện tại                                 |
| **status**                           | PENDING/IN_REVIEW/RETURNED/APPROVED/REJECTED/WITHDRAWN/EXPIRED     |
| **execution_status**                 | NOT_EXECUTED/EXECUTING/EXECUTED/FAILED                             |
| **correlation_id**                   | Uniqueidentifier unique để idempotency/tracing                     |
| **requested_reason**                 | Bắt buộc với thay đổi nhạy cảm                                     |
| workflow_id/workflow_version_id      | Bắt buộc; FK workflow/version đã chọn khi submit.                  |
| workflow_binding_id                  | Binding GLOBAL/COMPANY thực tế đã áp dụng.                         |
| workflow_snapshot                    | JSON chọn lọc gồm version, step, rule, SLA và assignee đã resolve. |
| workflow_snapshot_hash               | SHA-256 chống thay đổi snapshot sau submit.                        |
| resolution_status                    | RESOLVED/FAILED; chỉ tạo request chính thức khi RESOLVED.          |

## 9.8. Các ALTER quan trọng khác

| **Bảng**                        | **Thay đổi**                                                                                                         |
|---------------------------------|----------------------------------------------------------------------------------------------------------------------|
| **User_Role_Company**           | Bổ sung effective_from/effective_to/status; hỗ trợ role GLOBAL/Tổng công ty cho GROUP_CUSTOMER_DATA_ADMIN.           |
| **DepartmentPermissions**       | Tham chiếu permission_code; chỉ ALLOW nền; audit thay đổi.                                                           |
| **RolePermissions**             | Tham chiếu permission_code; quyền có hiệu lực qua User_Role_Company.                                                 |
| **User_Individual_Permissions** | Bổ sung effect=ALLOW/DENY, reason, effective_from/to, granted_by, revoked_by.                                        |
| **UserPermissions**             | Chỉ cache/view; bổ sung calculated_at, policy_version; invalidation khi nguồn quyền thay đổi.                        |
| **Customers**                   | Không dùng total_spent và assigned_staff_id làm nguồn chuẩn; notes công ty chuyển sang Customer_Company_Context.     |
| **Customer_Staff_History**      | Tham chiếu customer_company_context_id hoặc bổ sung company_id; unique current theo customer+company.                |
| **Customer_Care_Services**      | Bổ sung company_id, standard_price_snapshot, price_override_reason, price_approval_request_id, price_approved_by/at. |
| **Document_Files**              | Bổ sung data_scope GLOBAL/COMPANY và company_id NULL/NOT NULL theo scope.                                            |
| **Import_Logs**                 | Bổ sung company_id và kiểm tra quyền ADMIN_IMPORT/IMPORT_EXECUTE.                                                    |
| **System_Audit_Logs**           | Bảo đảm actor, entity, before/after, changed_fields, reason, correlation_id và thời gian; bất biến.                  |

## 9.9. Business_Process_Catalog

| **Trường**              | **Kiểu**         | **Null** | **Ràng buộc** | **Mô tả**                                     |
|-------------------------|------------------|----------|---------------|-----------------------------------------------|
| process_code            | varchar(100)     | NO       | PK            | Mã quy trình bất biến do DEV quản lý          |
| process_name            | nvarchar(200)    | NO       |               | Tên hiển thị                                  |
| module_code             | varchar(50)      | NO       | INDEX         | Module nghiệp vụ                              |
| entity_type             | varchar(100)     | NO       |               | Đối tượng thực thi                            |
| approval_mode           | varchar(20)      | NO       | CHECK         | NONE/OPTIONAL/REQUIRED                        |
| execution_handler_code  | varchar(100)     | NO       |               | Handler do DEV đăng ký; Admin không sửa       |
| condition_field_catalog | nvarchar(MAX)    | YES      | ISJSON        | Field/operator được phép dùng trong condition |
| is_active               | bit              | NO       | 1             | Chỉ process ACTIVE được binding               |
| created/updated         | audit+rowversion | NO/YES   |               | Audit                                         |

## 9.10. Approval_Workflows

| **Trường**       | **Kiểu**            | **Null** | **Ràng buộc**      | **Mô tả**                  |
|------------------|---------------------|----------|--------------------|----------------------------|
| id/workflow_code | bigint/nvarchar(50) | NO       | PK/UK              | Định danh workflow ổn định |
| process_code     | varchar(100)        | NO       | FK Process_Catalog | Chỉ gắn một process có sẵn |
| workflow_name    | nvarchar(200)       | NO       |                    | Tên quản trị               |
| description      | nvarchar(1000)      | YES      |                    | Mô tả                      |
| owner_scope_type | varchar(20)         | NO       | GLOBAL/COMPANY     | Phạm vi quản trị           |
| owner_company_id | theo Companies.id   | YES      | FK                 | NULL khi GLOBAL            |
| is_active        | bit                 | NO       | 1                  | Cho phép tạo version mới   |
| created/updated  | audit+rowversion    | NO/YES   |                    | Audit                      |

## 9.11. Approval_Workflow_Versions

| **Trường**                  | **Kiểu**               | **Null** | **Ràng buộc**              | **Mô tả**                      |
|-----------------------------|------------------------|----------|----------------------------|--------------------------------|
| id                          | bigint IDENTITY        | NO       | PK                         | Khóa version                   |
| workflow_id/version_no      | bigint/int             | NO       | UK(workflow_id,version_no) | Số phiên bản tăng dần          |
| status                      | varchar(20)            | NO       | CHECK                      | DRAFT/PUBLISHED/ACTIVE/RETIRED |
| effective_from/effective_to | datetime2(3)           | YES      |                            | Khoảng nhận request mới        |
| change_reason               | nvarchar(1000)         | NO       |                            | Lý do thay đổi                 |
| definition_hash             | char(64)               | NO       |                            | SHA-256 cấu hình publish       |
| published_by/at             | nvarchar(50)/datetime2 | YES      |                            | Người và thời điểm publish     |
| created/updated             | audit+rowversion       | NO/YES   |                            | DRAFT được sửa có concurrency  |

## 9.12. Approval_Workflow_Steps

| **Trường**          | **Kiểu**                  | **Null** | **Ràng buộc**       | **Mô tả**                     |
|---------------------|---------------------------|----------|---------------------|-------------------------------|
| id                  | bigint IDENTITY           | NO       | PK                  | Khóa step definition          |
| workflow_version_id | bigint                    | NO       | FK                  | Version cha                   |
| step_no             | smallint                  | NO       | UK(version,step_no) | Thứ tự tuần tự                |
| step_code/step_name | varchar(50)/nvarchar(200) | NO       |                     | Mã/tên snapshot               |
| required_permission | varchar(100)              | YES      | FK Permissions      | Permission hành động/ủy quyền |
| allow_return        | bit                       | NO       | 1                   | Cho phép trả về người gửi     |
| due_minutes         | int                       | YES      | \>0                 | SLA từ activated_at           |
| status              | varchar(20)               | NO       | ACTIVE              | ACTIVE/INACTIVE trong DRAFT   |
| created/updated     | audit+rowversion          | NO/YES   |                     | Audit                         |

## 9.13. Approval_Step_Approver_Rules

| **Trường**         | **Kiểu**         | **Null** | **Ràng buộc**        | **Mô tả**                                                                                                 |
|--------------------|------------------|----------|----------------------|-----------------------------------------------------------------------------------------------------------|
| id                 | bigint IDENTITY  | NO       | PK                   | Khóa rule                                                                                                 |
| workflow_step_id   | bigint           | NO       | FK                   | Step cha                                                                                                  |
| rule_type          | varchar(40)      | NO       | CHECK                | SPECIFIC_USER/ROLE/DEPARTMENT/DEPARTMENT_MANAGER/REQUESTER_MANAGER/PERMISSION/ADMIN_GROUP/DATA_FIELD_USER |
| rule_value         | nvarchar(500)    | YES      |                      | ID/code/field_code tương ứng                                                                              |
| company_scope_mode | varchar(20)      | NO       | REQUEST/GLOBAL/FIXED | Cách xác định company                                                                                     |
| exclude_requester  | bit              | NO       | 1                    | Bắt buộc chống self-approval                                                                              |
| is_active          | bit              | NO       | 1                    | Hiệu lực trong DRAFT                                                                                      |
| created/updated    | audit+rowversion | NO/YES   |                      | Audit                                                                                                     |

## 9.14. Approval_Step_Conditions

| **Trường**          | **Kiểu**           | **Null** | **Ràng buộc**     | **Mô tả**                          |
|---------------------|--------------------|----------|-------------------|------------------------------------|
| id                  | bigint IDENTITY    | NO       | PK                | Khóa condition                     |
| owner_type/owner_id | varchar(20)/bigint | NO       | STEP hoặc BINDING | Đối tượng điều kiện                |
| field_code          | varchar(100)       | NO       | whitelist         | Field do DEV công bố               |
| operator_code       | varchar(30)        | NO       | CHECK             | EQ/NE/GT/GTE/LT/LTE/IN/IS_NULL/... |
| compare_value       | nvarchar(1000)     | YES      |                   | Giá trị typed/JSON                 |
| group_no/order_no   | smallint           | NO       |                   | Nhóm AND/OR xác định               |
| created/updated     | audit+rowversion   | NO/YES   |                   | Audit                              |

## 9.15. Approval_Workflow_Bindings

| **Trường**          | **Kiểu**          | **Null** | **Ràng buộc**  | **Mô tả**                  |
|---------------------|-------------------|----------|----------------|----------------------------|
| id                  | bigint IDENTITY   | NO       | PK             | Khóa binding               |
| process_code        | varchar(100)      | NO       | FK             | Quy trình có sẵn           |
| workflow_version_id | bigint            | NO       | FK             | Chỉ PUBLISHED/ACTIVE       |
| scope_type          | varchar(20)       | NO       | GLOBAL/COMPANY | Phạm vi áp dụng            |
| company_id          | theo Companies.id | YES      | FK             | Bắt buộc khi COMPANY       |
| priority            | int               | NO       | \>=1           | Số nhỏ ưu tiên cao         |
| effective_from/to   | datetime2(3)      | NO/YES   |                | Khoảng áp dụng request mới |
| status              | varchar(20)       | NO       | ACTIVE         | DRAFT/ACTIVE/INACTIVE      |
| created/updated     | audit+rowversion  | NO/YES   |                | Chặn overlap khi publish   |

## 9.16. Approval_Reminder_Policies

| **Trường**                   | **Kiểu**         | **Null** | **Ràng buộc** | **Mô tả**                          |
|------------------------------|------------------|----------|---------------|------------------------------------|
| id                           | bigint IDENTITY  | NO       | PK            | Khóa policy                        |
| workflow_step_id             | bigint           | NO       | FK/UK         | Một policy cho một step            |
| before_due_minutes           | int              | YES      | \>=0          | Nhắc trước hạn                     |
| send_at_due                  | bit              | NO       | 0             | Nhắc đúng hạn                      |
| repeat_after_overdue_minutes | int              | YES      | \>0           | Chu kỳ nhắc quá hạn                |
| max_repeat_count             | int              | YES      | \>=0          | NULL là không giới hạn theo policy |
| notify_requester             | bit              | NO       | 0             | Có gửi người gửi hay không         |
| created/updated              | audit+rowversion | NO/YES   |               | Audit                              |

## 9.17. Approval_Request_Step_Assignees

| **Trường**         | **Kiểu**                    | **Null** | **Ràng buộc**     | **Mô tả**                     |
|--------------------|-----------------------------|----------|-------------------|-------------------------------|
| id                 | bigint IDENTITY             | NO       | PK                | Khóa assignee                 |
| request_step_id    | bigint                      | NO       | FK                | Step runtime                  |
| user_id            | nvarchar(50)                | NO       | FK Users          | Người đủ điều kiện            |
| source_rule_id     | bigint                      | YES      | FK Approver_Rules | Nguồn resolve                 |
| assignment_type    | varchar(20)                 | NO       | PRIMARY/DELEGATE  | Loại assignment               |
| is_active          | bit                         | NO       | 1                 | Có thể bị thay người có audit |
| resolved_at        | datetime2(3)                | NO       |                   | Thời điểm resolve             |
| replaced_by/reason | nvarchar(50)/nvarchar(1000) | YES      |                   | Thay người bước chờ           |
| created/updated    | audit+rowversion            | NO/YES   |                   | Unique active step+user       |

## 9.18. Approval_Reminder_Logs

| **Trường**              | **Kiểu**                    | **Null** | **Ràng buộc**       | **Mô tả**                                     |
|-------------------------|-----------------------------|----------|---------------------|-----------------------------------------------|
| id                      | bigint IDENTITY             | NO       | PK                  | Khóa log                                      |
| request_step_id/user_id | bigint/nvarchar(50)         | NO       | FK                  | Step và người nhận                            |
| reminder_type           | varchar(30)                 | NO       | CHECK               | BEFORE_DUE/AT_DUE/OVERDUE_REPEAT              |
| scheduled_at            | datetime2(3)                | NO       | INDEX               | Thời điểm dự kiến                             |
| sent_at                 | datetime2(3)                | YES      |                     | Thời điểm thực tế                             |
| status                  | varchar(20)                 | NO       | PENDING/SENT/FAILED | Trạng thái                                    |
| dedupe_key              | varchar(150)                | NO       | UNIQUE              | Chống gửi trùng                               |
| error_code/message      | varchar(100)/nvarchar(1000) | YES      |                     | Lỗi gửi                                       |
| created_at              | datetime2(3)                | NO       | sysdatetime()       | Không update/delete ngoài trạng thái delivery |

# 10. LUỒNG XỬ LÝ CHI TIẾT

## 10.1. Tạo khách hàng mới

1.  NV nghiệp vụ tìm kiếm customer master và chạy duplicate check.

2.  Gửi CREATE_CUSTOMER với after_data và company_id; tài liệu gắn entity_type=APPROVAL.

3.  Hệ thống resolve GROUP_CUSTOMER_DATA_ADMIN và tạo approval step.

4.  Người duyệt kiểm tra CCCD, họ tên, ngày sinh, phone và dữ liệu trùng.

5.  Khi APPROVED, execution transaction tạo Profiles, Customers, Customer_Company_Context; chuyển tài liệu sang CUSTOMER; ghi audit/action.

6.  Nếu duplicate xuất hiện trước execution, chuyển FAILED/CONFLICT và không tạo bản ghi thứ hai.

## 10.2. Thay đổi customer master

1.  NV gửi CUSTOMER_MASTER_CHANGE với before_data, after_data và target_version.

2.  Nhóm quản trị xem diff field-level; có thể RETURN để bổ sung.

3.  Khi duyệt, hệ thống kiểm tra target_version; nếu đổi thì RETURNED/CONFLICT.

4.  Cập nhật Profiles/Customers, ghi audit, action và thông báo người đề nghị/các context liên quan.

## 10.3. Giá dịch vụ ngoại lệ

1.  Khi tạo/gia hạn, hệ thống snapshot giá tiêu chuẩn vào standard_price_snapshot.

2.  Nếu price bằng standard snapshot, cho phép tiếp tục không duyệt.

3.  Nếu khác, tạo SERVICE_PRICE_OVERRIDE và khóa trạng thái bán/bill cho đến APPROVED.

4.  Trưởng BP hoặc delegate ACTIVE có thể xử lý. Người gửi không được tự duyệt.

5.  Execution ghi giá duyệt, reason, approval_request_id, approved_by/at và Care_Service_History.

6.  Nếu giá bị sửa sau khi duyệt, approval cũ hết hiệu lực; tạo request mới.

## 10.4. Duyệt tuần tự với delegation

1.  Step được tạo theo workflow snapshot; step_no=1 là PENDING, các step sau là WAITING. Toàn bộ assignee được resolve khi submit.

2.  Queue chỉ hiển thị step PENDING cho assignee gốc và delegate ACTIVE phù hợp; step WAITING không cho action.

3.  Khi một người action, transaction kiểm tra step.status=PENDING, assignee ACTIVE, row_version và self-approval.

4.  Nếu actor là delegate, lưu on_behalf_of và delegation_id.

5.  Commit action + step; nếu APPROVE thì kích hoạt step kế tiếp từ WAITING sang PENDING. Người còn lại không thể action lại.

## 10.5. Admin sửa payment CONFIRMED

1.  Admin có ADMIN_PAYMENT/PAYMENT_CORRECT_CONFIRMED mở màn hình correction và nhập reason.

2.  Client gửi correction package gồm row_version, danh sách header/item trước/sau và correlation_id.

3.  Stored procedure khóa payment, kiểm tra quyền/invariant, thu thập kỳ đối soát cũ và tính dữ liệu mới.

4.  Cập nhật payment/items, customer context, aggregates và reconciliation flags trong một transaction.

5.  Ghi System_Audit_Logs before/after/changed_fields; commit.

6.  Sau commit, tạo Notifications cho người thu, PTKD, Kế toán và công ty cũ/mới nếu có.

## 10.6. Admin tạo và publish workflow

1. Admin chọn process_code từ Business_Process_Catalog và scope quản trị.

2. Tạo workflow/version DRAFT, cấu hình step tuần tự, approver rule, condition và reminder.

3. Hệ thống validate: step_no liên tục, rule hợp lệ, field/operator whitelist, không self-reference, không binding overlap và process có handler.

4. Admin có WORKFLOW_PUBLISH publish version; hệ thống tính definition_hash và khóa cấu trúc.

5. Admin tạo binding GLOBAL hoặc COMPANY, đặt priority/effective period và kích hoạt.

## 10.7. Submit và resolve approval plan

1. Nghiệp vụ gọi Approval.ResolvePlan(process_code, company_id, payload, requested_by).

2. Hệ thống chọn binding COMPANY trước GLOBAL, đánh giá condition và lấy version hiệu lực.

3. Resolve assignee cho tất cả step, loại requested_by và user không ACTIVE/ngoài scope.

4. Nếu bất kỳ step không có assignee, trả APPROVAL_APPROVER_NOT_RESOLVED; không tạo request chính thức.

5. Nếu hợp lệ, transaction tạo Approval_Requests, snapshot/hash, step vòng 1, assignee và action SUBMIT; chỉ step 1 PENDING.

## 10.8. RETURN, chỉnh sửa và gửi lại

1. Approver nhập action_note và RETURN; request chuyển RETURNED, step hiện tại RETURNED, step tương lai CANCELLED.

2. Người gửi chỉnh payload; hệ thống so sánh target_version và cập nhật after_data/payload_hash cho round mới.

3. RESUBMIT tăng current_round_no, tạo lại step/assignee bằng workflow_version_id ban đầu và ghi action RESUBMIT.

4. Nếu target entity đã thay đổi, trả APPROVAL_TARGET_VERSION_CONFLICT và không ghi đè.

## 10.9. Nhắc hạn và quá hạn

1. Khi step chuyển PENDING, hệ thống tính due_at và lịch reminder từ policy snapshot.

2. Job tìm lịch đến hạn, tạo reminder log với dedupe_key rồi mới gửi notification.

3. Quá due_at, cập nhật is_overdue=1; step và assignee không thay đổi.

4. Không có chức năng tự động chuyển cấp trong v1.1.

## 10.10. Thay đổi workflow khi có request đang chạy

- Publish version mới không cập nhật workflow_version_id, step hoặc assignee của request đã tồn tại.

- Binding mới chỉ được chọn cho request submit từ effective_from trở đi.

- v1.1 không cho migrate request đang chạy. Trường hợp đặc biệt xử lý bằng WITHDRAW và tạo request mới, không sửa lịch sử.

# 11. API/SERVICE VÀ KIỂM SOÁT DATABASE

## 11.1. Các operation logic

| **Operation**                  | **Input chính**                                     | **Output**                                     |
|--------------------------------|-----------------------------------------------------|------------------------------------------------|
| **Authorization.Check**        | user_id, permission_code, company_id, entity/status | allow/deny + reason + policy_version           |
| **Customer.ProposeCreate**     | after_data, company_id, documents                   | Approval request                               |
| **Customer.ProposeChange**     | target_id/version, before/after                     | Approval request                               |
| **CustomerAdmin.UpdateDirect** | entity/version, changes, reason                     | Updated + audit                                |
| **Approval.Submit**            | process_code, company_id, payload, requester        | resolved request + snapshot + sequential steps |
| **Approval.Act**               | request/step/version, action, note                  | step/request/action updated                    |
| **Approval.Execute**           | request_id, payload_hash                            | entity update idempotent                       |
| **Delegation.Create**          | delegator, delegate, permission, dates              | PENDING_ACCEPTANCE                             |
| **Delegation.Accept**          | delegation/version                                  | PENDING_ADMIN                                  |
| **Delegation.AdminActivate**   | delegation/version, admin                           | SCHEDULED/ACTIVE                               |
| **Payment.Confirm**            | payment/version                                     | CONFIRMED                                      |
| **Payment.AdminCorrect**       | correction package + reason                         | updated + audit + notifications                |
| Workflow.ProcessCatalog.List   | module/scope/filter                                 | Danh sách process Admin được phép bind         |
| Workflow.CreateDraft           | process_code, scope, name                           | Workflow + version DRAFT                       |
| Workflow.SaveDefinition        | version, steps, rules, conditions, reminders        | DRAFT updated + validation result              |
| Workflow.Publish               | version, row_version, reason                        | PUBLISHED/ACTIVE + definition_hash             |
| Workflow.Bind                  | process, version, scope, company, priority, dates   | Binding ACTIVE hoặc validation errors          |
| Approval.ResolvePlan           | process, company, payload, requester                | Selected binding/version + assignee plan       |
| Approval.ReassignPendingStep   | step/version, new_user, reason                      | Assignee replacement + audit                   |
| Reminder.Dispatch              | schedule window                                     | Reminder logs + notifications                  |

## 11.2. Stored procedure/DB control đề xuất

| **Thành phần**                       | **Mục đích**                                                                                                     |
|--------------------------------------|------------------------------------------------------------------------------------------------------------------|
| **sp_Payment_Confirm**               | Khóa DRAFT, kiểm tra item/tổng/field bắt buộc, chuyển CONFIRMED và cập nhật aggregates.                          |
| **sp_AdminCorrectConfirmedPayment**  | Hiệu chỉnh header/items có reason, audit, reconciliation impact và optimistic concurrency.                       |
| **sp_Approval_Act**                  | Atomic step transition; self-approval check; delegation check; ghi Approval_Actions.                             |
| **sp_Approval_Execute**              | Kiểm tra APPROVED + payload_hash + target_version; thực thi idempotent.                                          |
| **sp_Delegation_Activate**           | Chỉ DELEGATION_ACTIVATE; kiểm tra acceptance, dates, permission delegable và overlap.                            |
| **job_Delegation_Status**            | Chuyển SCHEDULED→ACTIVE và ACTIVE→EXPIRED theo thời gian; chạy idempotent.                                       |
| **sp_Customer_CreateFromApproval**   | Duplicate check lần cuối; tạo Profile/Customer/Context và relink documents.                                      |
| **sp_Customer_UpdateFromApproval**   | Version check; field diff; cập nhật master và audit.                                                             |
| **trg_Payment_BlockDeleteConfirmed** | Chặn DELETE/is_deleted đối với CONFIRMED.                                                                        |
| **trg_Audit_Immutable**              | Chặn update/delete audit/action history nếu chính sách DB sử dụng trigger.                                       |
| sp_Workflow_ValidateAndPublish       | Kiểm tra process, step tuần tự, rule/condition whitelist, overlap, effective dates; khóa definition và hash.     |
| sp_Approval_SubmitResolved           | Chọn binding/version, resolve toàn bộ assignee, chặn thiếu người duyệt, tạo request/snapshot/step/action atomic. |
| sp_Approval_ReassignPendingStep      | Chỉ step PENDING; kiểm tra quyền, người mới, self-approval; đóng assignment cũ và ghi audit/action.              |
| job_Approval_Reminders               | Tạo reminder log idempotent, gửi notification, đánh dấu overdue; không chuyển cấp.                               |
| job_Workflow_Version_Status          | Chuyển PUBLISHED-\>ACTIVE và ACTIVE-\>RETIRED theo thời gian, không tác động request cũ.                         |

## 11.3. Error code tối thiểu

| **Mã**                           | **Tình huống**                                       | **HTTP gợi ý** |
|----------------------------------|------------------------------------------------------|----------------|
| AUTH_PERMISSION_DENIED           | Không có permission                                  | 403            |
| AUTH_COMPANY_SCOPE_DENIED        | Ngoài company scope                                  | 403            |
| APPROVAL_SELF_ACTION             | Người gửi tự duyệt                                   | 409/403        |
| APPROVAL_STEP_ALREADY_ACTED      | Step đã được người khác xử lý                        | 409            |
| APPROVAL_TARGET_VERSION_CONFLICT | Entity đã thay đổi                                   | 409            |
| DELEGATION_NOT_ACTIVE            | Ủy quyền chưa ACTIVE/hết hạn                         | 403            |
| DELEGATION_ADMIN_REQUIRED        | Chưa được Admin kích hoạt                            | 409            |
| CUSTOMER_DUPLICATE               | Phát hiện khách hàng trùng                           | 409            |
| PAYMENT_IMMUTABLE_FIELD          | Sửa id/bill_code/status/currency                     | 422            |
| PAYMENT_TOTAL_MISMATCH           | Tổng header khác tổng item                           | 422            |
| PAYMENT_ROWVERSION_CONFLICT      | Payment đã bị sửa                                    | 409            |
| WORKFLOW_PROCESS_NOT_SUPPORTED   | process_code không có/không ACTIVE                   | 422            |
| WORKFLOW_DEFINITION_INVALID      | Step/rule/condition/reminder không hợp lệ            | 422            |
| WORKFLOW_BINDING_NOT_FOUND       | Không có binding hiệu lực cho process bắt buộc duyệt | 409            |
| WORKFLOW_BINDING_OVERLAP         | Binding trùng scope/condition/effective period       | 409            |
| WORKFLOW_VERSION_IMMUTABLE       | Sửa version đã publish/active                        | 409            |
| APPROVAL_APPROVER_NOT_RESOLVED   | Một step không tìm được người duyệt hợp lệ           | 409            |
| APPROVAL_STEP_NOT_CURRENT        | Action vào step WAITING/CANCELLED/không hiện tại     | 409            |
| APPROVAL_REASSIGN_INVALID        | Người thay thế không hợp lệ hoặc là requester        | 422            |

# 12. AUDIT, THÔNG BÁO VÀ DỮ LIỆU NHẠY CẢM

## 12.1. Sự kiện phải audit

- Cấp/thu hồi role, quyền cá nhân và group Admin.

- Thay đổi DepartmentPermissions/RolePermissions/Permissions catalog.

- Tạo, kích hoạt, từ chối, thu hồi và hết hạn delegation.

- Mọi approval action và execution retry/failure.

- Tạo/sửa/gộp customer master; xem/xuất dữ liệu nhạy cảm khi policy yêu cầu.

- Xác nhận và hiệu chỉnh payment; thay đổi kỳ đối soát.

- Import, rollback, export và download tài liệu nhạy cảm.

- Tạo/sửa DRAFT, validate, publish, retire workflow version; tạo/kích hoạt/vô hiệu binding.

- Resolve approval plan thất bại, thiếu approver, thay người xử lý step PENDING và thay đổi reminder policy.

- Mọi reminder SENT/FAILED và thay đổi is_overdue quan trọng theo policy vận hành.

## 12.2. Audit payload

| **Trường**                 | **Yêu cầu**                                                        |
|----------------------------|--------------------------------------------------------------------|
| **actor_id / acting_as**   | Người thực tế và người được đại diện                               |
| **entity_type/entity_id**  | Đối tượng bị tác động                                              |
| **company_id**             | Scope nghiệp vụ                                                    |
| **action**                 | Mã hành động ổn định                                               |
| **before_data/after_data** | Chỉ trường cần thiết; JSON hợp lệ; bảo vệ PII                      |
| **changed_fields**         | Danh sách field thay đổi                                           |
| **reason**                 | Bắt buộc với customer master, payment correction và quyền nhạy cảm |
| **correlation_id**         | Liên kết API → procedure → audit → notification                    |
| **created_at**             | datetime2(3); không update/delete                                  |

## 12.3. Ma trận thông báo

| **Sự kiện**                          | **Người nhận**                                    | **Thời điểm**            |
|--------------------------------------|---------------------------------------------------|--------------------------|
| Customer request submitted           | Nhóm quản trị dữ liệu                             | Sau submit               |
| Customer request result              | Người gửi và người phụ trách company context      | Sau quyết định/execution |
| Customer master direct edit          | Người phụ trách các context liên quan theo policy | Sau commit               |
| Approval assigned/returned           | Primary approver/delegate/người gửi               | Theo sự kiện             |
| Delegation requested                 | Delegate                                          | Sau tạo                  |
| Delegation pending admin             | Admin Security                                    | Sau delegate accept      |
| Delegation active/expired/revoked    | Delegator + delegate                              | Sau chuyển trạng thái    |
| Payment corrected                    | Thu ngân, PTKD, Kế toán; công ty cũ/mới           | Sau commit               |
| Reconciliation impacted              | Nhóm đối soát                                     | Sau payment correction   |
| Workflow published/binding activated | Admin workflow và chủ sở hữu nghiệp vụ theo scope | Sau commit               |
| Approval approver unresolved         | Người gửi + Admin workflow                        | Ngay khi submit bị chặn  |
| Approval step due reminder           | Assignee/Delegate và người gửi nếu policy chọn    | Trước/đúng hạn/quá hạn   |
| Approval step reassigned             | Người cũ, người mới, người gửi và Admin workflow  | Sau commit               |

## 12.4. Dữ liệu nhạy cảm

- CCCD, địa chỉ pháp lý, tài khoản ngân hàng và file định danh phải được mask theo permission.

- before_data/after_data không được chứa token, mật khẩu, URL ký vĩnh viễn hoặc file bytes.

- Document GLOBAL vẫn yêu cầu CUSTOMER_VIEW_SENSITIVE; GLOBAL chỉ mô tả scope sở hữu, không tự cấp quyền xem.

- Export dữ liệu nhạy cảm phải ghi purpose, filter, record_count và người thực hiện.

- Log/audit không cho user nghiệp vụ sửa hoặc xóa; chỉ truy cập qua quyền AUDIT_VIEW thích hợp.

# 13. MIGRATION VÀ KẾ HOẠCH TRIỂN KHAI

## 13.1. Trình tự

1.  Khảo sát schema thực tế của Users, Roles, User_Role_Company, DepartmentPermissions, RolePermissions, User_Individual_Permissions, Companies và Profiles/Customers.

2.  Tạo Permissions và seed permission catalog; mapping quyền cũ sang permission_code mới.

3.  Tạo Customer_Company_Context; backfill từ service/payment/assigned staff hiện có và xác định notes công ty.

4.  Tạo Approval_Request_Steps, Approval_Actions, Approval_Delegations; nâng cấp Approval_Requests.

5.  Bổ sung company_id/scope/price fields cho các bảng liên quan; chưa bật NOT NULL trước khi backfill sạch.

6.  Chuyển Customers.assigned_staff_id và notes theo công ty sang Customer_Company_Context; đối chiếu unique current staff.

7.  Chuyển total_spent sang view theo company; đối chiếu doanh thu theo payment CONFIRMED.

8.  Triển khai authorization service/cache invalidation và stored procedures quan trọng.

9.  Chạy test bằng dữ liệu gần production; bật feature flag theo module; giám sát deny/error/audit.

10. Sau giai đoạn ổn định, ngừng đọc các cột deprecated theo change request riêng; không xóa dữ liệu lịch sử vội.

11. Tạo Business_Process_Catalog và seed toàn bộ process_code đang dùng; xác định approval_mode và execution_handler_code.

12. Chuyển các rule người duyệt cố định hiện hữu thành workflow version 1 và binding GLOBAL/COMPANY tương ứng.

13. Tạo reminder policy mặc định, job reminder và dashboard quá hạn; chưa bật gửi thật trước khi test notification.

14. Feature flag workflow động theo từng process; request legacy tiếp tục dùng dữ liệu cũ hoặc snapshot legacy có giải thích.

## 13.2. Backfill bắt buộc

| **Hạng mục**          | **Điều kiện đạt**                                                                               |
|-----------------------|-------------------------------------------------------------------------------------------------|
| **Permissions**       | 100% quyền hiện hữu được mapping hoặc có quyết định loại bỏ; không có permission string mồ côi. |
| **Company scope**     | Mọi service/payment/approval/document/import xác định được company hoặc GLOBAL rõ ràng.         |
| **Customer context**  | Không trùng customer+company; assigned staff và notes được đối chiếu.                           |
| **Customer spending** | Tổng theo company từ view khớp SUM payment CONFIRMED.                                           |
| **Approval history**  | Request cũ được chuyển action/step hợp lý hoặc đánh dấu legacy có giải thích.                   |
| **Delegation**        | Không có delegation legacy được ACTIVE nếu chưa có Admin activation/audit.                      |
| **Payment**           | Không mất audit; correction procedure chặn immutable fields và phát notification.               |
| Process catalog       | 100% request_type hiện hữu mapping process_code hoặc có quyết định loại bỏ.                     |
| Workflow definition   | Mỗi process bắt buộc duyệt có ít nhất một version/binding hợp lệ; không overlap.                |
| Approver resolution   | Dữ liệu quản lý, role, department, permission đủ để resolve; lỗi được thống kê trước go-live.   |
| Legacy approvals      | Request đang chạy giữ luồng cũ/snapshot legacy; không tự migrate sang v1.1.                     |
| Reminder              | Job idempotent, timezone đúng, không gửi trùng và không tự chuyển cấp.                          |

## 13.3. Rollback release

Rollback ứng dụng không được xóa các bảng mới hoặc audit phát sinh. Khi buộc quay lại phiên bản cũ, các object mới được giữ read-only để điều tra; payment CONFIRMED và approval actions phát sinh không bị xóa. Script rollback schema chỉ được chạy khi đã có mapping dữ liệu ngược và chữ ký của DBA/chủ sở hữu nghiệp vụ.

# 14. TIÊU CHÍ NGHIỆM THU

| **Mã**      | **Điều kiện nghiệm thu**                                                                                                                       |
|-------------|------------------------------------------------------------------------------------------------------------------------------------------------|
| **AUTH-01** | NV PTKD nhận đúng quyền nền phòng ban sau khi đăng nhập.                                                                                       |
| **AUTH-02** | Role CASHIER chỉ có hiệu lực tại company_id được gán.                                                                                          |
| **AUTH-03** | Individual DENY chặn hành động dù department/role có ALLOW.                                                                                    |
| **AUTH-04** | User ở Công ty A không xem service/payment/document COMPANY của Công ty B.                                                                     |
| **AUTH-05** | Customer master GLOBAL được tìm kiếm theo permission; field nhạy cảm bị mask đúng.                                                             |
| **AUTH-06** | UserPermissions cache được làm mới khi role/department/individual permission thay đổi.                                                         |
| **CUS-01**  | NV thường không sửa được full_name, cccd, dob, phone hoặc contact_address.                                                                     |
| **CUS-02**  | NV tạo CREATE_CUSTOMER; chỉ nhóm quản trị thực thi bản ghi chính thức.                                                                         |
| **CUS-03**  | CREATE_CUSTOMER bị chặn khi duplicate check cuối phát hiện CCCD trùng.                                                                         |
| **CUS-04**  | CUSTOMER_MASTER_CHANGE xung đột target_version không ghi đè dữ liệu mới.                                                                       |
| **CUS-05**  | Nhóm quản trị sửa trực tiếp phải nhập reason và có before/after audit.                                                                         |
| **CUS-06**  | Customer_Company_Context unique theo customer+company và không lộ internal_notes chéo công ty.                                                 |
| **CUS-07**  | Tổng chi tiêu theo công ty khớp payment CONFIRMED; tổng tập đoàn chỉ hiện với permission riêng.                                                |
| **PAY-01**  | Thu ngân được tự tạo và tự xác nhận bill hợp lệ mà không có Approval_Requests.                                                                 |
| **PAY-02**  | User không có PAYMENT_CONFIRM không xác nhận được bill.                                                                                        |
| **PAY-03**  | Bill không item hoặc tổng không khớp bị chặn.                                                                                                  |
| **PAY-04**  | Thu ngân không sửa/xóa payment sau CONFIRMED.                                                                                                  |
| **PAY-05**  | ADMIN_PAYMENT sửa được company/customer/service/amount/date/method trong correction package hợp lệ.                                            |
| **PAY-06**  | Admin không đổi được id, bill_code, status CONFIRMED hoặc currency VND.                                                                        |
| **PAY-07**  | Đổi company/date đánh dấu đủ kỳ ngày/tháng cũ/mới; tổng đối soát được tính lại.                                                                |
| **PAY-08**  | Sau correction có audit before/after, reason và notification đúng người nhận.                                                                  |
| **APR-01**  | Người gửi không được tự duyệt request của mình.                                                                                                |
| **APR-02**  | Gia hạn đúng giá không tạo approval; price khác standard bắt buộc SERVICE_PRICE_OVERRIDE.                                                      |
| **APR-03**  | Giá ngoại lệ chưa APPROVED không được dùng để tạo/xác nhận bill.                                                                               |
| **APR-04**  | RETURNED có thể resubmit vòng mới và giữ nguyên lịch sử vòng cũ.                                                                               |
| **APR-05**  | Hai approver action đồng thời: chỉ một transaction thành công; transaction còn lại nhận conflict.                                              |
| **APR-06**  | Execution FAILED có thể retry idempotent với cùng payload_hash; không áp dụng hai lần.                                                         |
| **DEL-01**  | Delegation chưa được delegate accept/Admin activate không cấp quyền.                                                                           |
| **DEL-02**  | Người nhận không cần role tương đương nhưng chỉ duyệt đúng permission được ủy quyền.                                                           |
| **DEL-03**  | Trong ACTIVE, primary approver và delegate cùng thấy request; một người xử lý thì đóng step cho cả hai.                                        |
| **DEL-04**  | Delegate không tự duyệt request do chính mình tạo và không ủy quyền tiếp.                                                                      |
| **DEL-05**  | Đến effective_to tự chuyển EXPIRED; primary approver giữ nguyên quyền, không cần Admin kích hoạt lại.                                          |
| **DEL-06**  | Audit lưu acted_by, on_behalf_of và delegation_id.                                                                                             |
| **SEC-01**  | Không endpoint nào chỉ dựa vào quyền hiển thị UI.                                                                                              |
| **SEC-02**  | Audit/Approval_Actions không update/delete bởi user nghiệp vụ.                                                                                 |
| **SEC-03**  | Dữ liệu nhạy cảm trong log/export/document được mask và giới hạn đúng permission.                                                              |
| WFC-01      | Admin chỉ chọn được process_code ACTIVE trong Business_Process_Catalog; không tạo process/form mới.                                            |
| WFC-02      | Workflow version DRAFT sửa được; PUBLISHED/ACTIVE không sửa/xóa cấu trúc.                                                                      |
| WFC-03      | Luồng chỉ chạy tuần tự; mỗi request/round chỉ có tối đa một step PENDING.                                                                      |
| WFC-04      | Resolve được approver theo SPECIFIC_USER, ROLE, DEPARTMENT, DEPARTMENT_MANAGER, REQUESTER_MANAGER, PERMISSION, ADMIN_GROUP và DATA_FIELD_USER. |
| WFC-05      | ROLE/DEPARTMENT/PERMISSION có nhiều assignee nhưng chỉ action đầu tiên commit; người còn lại nhận conflict.                                    |
| WFC-06      | Không tìm được approver ở bất kỳ step nào thì chặn submit, không tạo request và thông báo người gửi/Admin.                                     |
| WFC-07      | Binding COMPANY được ưu tiên hơn GLOBAL; GLOBAL được dùng khi không có COMPANY phù hợp.                                                        |
| WFC-08      | Không publish được binding overlap cùng process/scope/condition/effective period/priority.                                                     |
| WFC-09      | RETURN luôn về người gửi; step tương lai CANCELLED; RESUBMIT tăng round_no và giữ lịch sử.                                                     |
| WFC-10      | RESUBMIT giữ workflow_version_id ban đầu dù đã có version mới.                                                                                 |
| WFC-11      | Request đang chạy không đổi step/assignee khi version hoặc binding mới được publish.                                                           |
| WFC-12      | Request mới sau effective_from sử dụng version mới; request trước thời điểm đó dùng version cũ.                                                |
| WFC-13      | Reminder gửi đúng trước/đúng/quá hạn, có dedupe log và không tự chuyển cấp.                                                                    |
| WFC-14      | Step quá hạn giữ PENDING, is_overdue=1 và vẫn chỉ assignee/delegate hợp lệ được action.                                                        |
| WFC-15      | Thay người step PENDING yêu cầu WORKFLOW_REASSIGN_PENDING, reason, chống self-approval và ghi audit.                                           |
| WFC-16      | Admin không thể cấu hình SQL/JavaScript hoặc field/operator ngoài whitelist.                                                                   |

> ĐIỀU KIỆN GO-LIVE Chỉ go-live khi toàn bộ test AUTH, CUS, PAY, APR, DEL, WFC và SEC đạt; process catalog, workflow binding/version, approver resolution, mapping quyền, company scope, payment/reconciliation và reminder được CNTT, PTKD và Kế toán xác nhận.

# PHỤ LỤC A. MÃ TRẠNG THÁI

| **Nhóm**                  | **Giá trị**                                                                                                                    |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------------|
| **Approval request**      | PENDING, IN_REVIEW, RETURNED, APPROVED, REJECTED, WITHDRAWN, EXPIRED                                                           |
| **Approval execution**    | NOT_EXECUTED, EXECUTING, EXECUTED, FAILED                                                                                      |
| **Approval step**         | WAITING, PENDING, APPROVED, REJECTED, RETURNED, CANCELLED, EXPIRED                                                             |
| **Approval action**       | SUBMIT, ASSIGN, START_REVIEW, APPROVE, REJECT, RETURN, RESUBMIT, WITHDRAW, REASSIGN, REMIND, EXPIRE, EXECUTE, EXECUTION_FAILED |
| **Delegation**            | PENDING_ACCEPTANCE, PENDING_ADMIN, SCHEDULED, ACTIVE, EXPIRED, REVOKED, REJECTED, DECLINED                                     |
| **Permission effect**     | ALLOW, DENY                                                                                                                    |
| **Permission scope**      | GLOBAL, COMPANY                                                                                                                |
| **Customer relationship** | ACTIVE, INACTIVE                                                                                                               |
| **Document scope**        | GLOBAL, COMPANY                                                                                                                |
| Workflow version          | DRAFT, PUBLISHED, ACTIVE, RETIRED                                                                                              |
| Workflow binding          | DRAFT, ACTIVE, INACTIVE                                                                                                        |
| Approver rule             | SPECIFIC_USER, ROLE, DEPARTMENT, DEPARTMENT_MANAGER, REQUESTER_MANAGER, PERMISSION, ADMIN_GROUP, DATA_FIELD_USER               |
| Reminder log              | PENDING, SENT, FAILED                                                                                                          |

# PHỤ LỤC B. CHECKLIST CHO DEV/DBA/QA

☐ Đã seed Permissions và mapping mọi nguồn quyền hiện hữu.

☐ Đã chốt kiểu PK thật của Companies.id và các FK liên quan.

☐ Đã thực hiện company scope trên tất cả API/service nghiệp vụ.

☐ Đã tách customer master GLOBAL khỏi context/transaction COMPANY.

☐ Đã chặn nhân viên thường sửa mọi field customer master, kể cả phone/address.

☐ Đã có CREATE_CUSTOMER và CUSTOMER_MASTER_CHANGE với target_version/payload hash.

☐ Đã xử lý delegation additive, Admin activation và auto-expiry idempotent.

☐ Đã chống self-approval và double-action concurrency.

☐ Đã cho CASHIER tự confirm payment nhưng khóa mọi sửa sau CONFIRMED.

☐ Đã kiểm tra payment correction dây chuyền và notification sau commit.

☐ Đã xác nhận audit/action history bất biến và bảo vệ PII.

☐ Đã chạy toàn bộ acceptance tests bằng dữ liệu staging gần production.

- Đã seed Business_Process_Catalog và khóa quyền tạo process/form mới cho Admin.

- Đã có workflow versioning/binding GLOBAL-COMPANY và kiểm tra overlap.

- Đã resolve toàn bộ assignee trước submit và chặn APPROVAL_APPROVER_NOT_RESOLVED.

- Đã bảo đảm chỉ một step PENDING, RETURN về người gửi và RESUBMIT giữ version cũ.

- Đã triển khai reminder idempotent, overdue không chuyển cấp và audit đầy đủ.

- Đã kiểm thử version mới không tác động request đang chạy.

# PHỤ LỤC C. LỊCH SỬ THAY ĐỔI

| **Phiên bản** | **Ngày**   | **Nội dung**                                                                                                                 | **Trạng thái** |
|---------------|------------|------------------------------------------------------------------------------------------------------------------------------|----------------|
| 1.0           | 13/07/2026 | Baseline phân quyền, approval runtime, delegation, payment correction.                                                       | Đã thay thế    |
| 1.1           | 14/07/2026 | Bổ sung workflow động do Admin cấu hình cho process có sẵn; tuần tự; GLOBAL/COMPANY; versioning; reminder; resolve approver. | Bản hiện hành  |

> KẾT THÚC TÀI LIỆU Đặc tả v1.1 là nguồn chuẩn triển khai phân quyền, quản trị khách hàng, payment control, phê duyệt động, versioning, binding, ủy quyền và reminder của hệ thống PTKD. Mọi thay đổi khác phải có change request và version tài liệu mới.
