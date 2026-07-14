# Project Readiness Review

## 1. Phạm vi nghiệp vụ đã chốt
Phạm vi nghiệp vụ cơ bản đã được xác định rõ ràng thông qua tài liệu đặc tả và các quy tắc nghiệp vụ:
- **Phân quyền & Phạm vi dữ liệu (Scope):** Quyền được phân giải thông qua phòng ban (quyền nền), role tại công ty (quyền bổ sung) và quyền cá nhân. Dữ liệu chia làm 2 cấp: `GLOBAL` (Customer Master, Profiles) và `COMPANY` (Customer_Company_Context, Dịch vụ, Thanh toán).
- **Quản trị Khách hàng:** Nhân viên nghiệp vụ chỉ được đề xuất thay đổi (`CREATE_CUSTOMER`, `CUSTOMER_MASTER_CHANGE`). Việc cập nhật chính thức, gộp dữ liệu trùng lặp (`CUSTOMER_MERGE_DUPLICATE`) do nhóm Quản trị dữ liệu thực hiện.
- **Thanh toán & Thu tiền:** Thu ngân có thể tự tạo và xác nhận (`PAYMENT_CONFIRM`). Sau khi xác nhận, chỉ Admin thanh toán mới được hiệu chỉnh có giới hạn (không sửa `bill_code`, `status`, `currency_code`) và phải ghi log/thông báo.
- **Quy trình phê duyệt (Workflow):** Cấu hình động bởi Admin (nhưng chỉ cho các quy trình đã lập trình). Duyệt theo tuần tự (Sequential). Có quản lý phiên bản (DRAFT, PUBLISHED, ACTIVE, RETIRED) và phân cấp binding (`COMPANY` ưu tiên hơn `GLOBAL`).
- **Ủy quyền (Delegation):** Người ủy quyền giữ quyền gốc. Ủy quyền chỉ có hiệu lực với các bước phê duyệt và phải được Admin kích hoạt. Tự động hết hạn.
- **Kiểm soát Bảo mật & Audit:** Mọi hành động nhạy cảm, thay đổi dữ liệu thanh toán hay khách hàng đều phải ghi nhận audit (trước/sau). Che giấu thông tin nhạy cảm theo phân quyền.

## 2. Các quyết định kỹ thuật đã có
Dựa trên `AGENTS.md`, các quyết định kỹ thuật nền tảng bao gồm:
- **Loại ứng dụng:** Ứng dụng Web nội bộ (Internal web application).
- **Backend:** ASP.NET Core Web API. Tiền tố API công khai là `/api/v2`.
- **Frontend:** React với TypeScript.
- **Database:** Microsoft SQL Server (database ban đầu: `PTKD_DEV`).
- **Concurrency:** Sử dụng cơ chế khóa lạc quan (Optimistic concurrency) với `rowversion` cho các nghiệp vụ yêu cầu tính toàn vẹn cao.
- **Transaction:** Bắt buộc dùng giao dịch (transaction) cho thao tác nghiệp vụ nguyên tử (thanh toán, workflow, thay đổi dữ liệu nhạy cảm).
- **Triển khai Database:** Script SQL forward/rollback phải được đánh phiên bản và lưu trong thư mục `database/`.
- **Quy tắc API:** Trả về định dạng Problem Details chuẩn, mã lỗi nghiệp vụ ổn định. Không trả về Entities trực tiếp, phải dùng DTOs.
- **Security:** Không đưa raw SQL/JS vào cấu hình workflow condition. Phân quyền và validate bảo mật bắt buộc kiểm tra tại Backend, không phụ thuộc vào ẩn/hiện UI.

## 3. Các quyết định còn thiếu
Để có thể bắt đầu lập trình, các quyết định sau cần được làm rõ:
- **ORM / DB Access Framework:** Backend sẽ sử dụng Entity Framework Core, Dapper hay ADO.NET thuần?
- **UI Component Library:** Frontend sẽ sử dụng thư viện UI nào (Ví dụ: Material-UI, Ant Design, Tailwind CSS)?
- **State Management:** Frontend dùng Redux, Zustand hay React Context? Data fetching dùng React Query hay Apollo?
- **Testing Frameworks:** Cho backend (xUnit/NUnit/MSTest + Moq/NSubstitute) và frontend (Jest/Vitest, React Testing Library, Cypress/Playwright)?
- **Logging & Tracing:** Backend sử dụng Serilog hay NLog? Định dạng log (nhất là JSON Audit log) lưu trữ vào bảng SQL nào hay xuất ra ElasticSearch/Logstash?
- **Môi trường & CI/CD:** Kế hoạch cấu hình CI/CD và môi trường UAT chưa được xác định rõ (tài liệu chỉ rõ IIS/Production là ngoài phạm vi hiện tại, nhưng chưa rõ UAT test ở đâu).

## 4. Kiến trúc đề xuất cho SQL Server, Web và API v2
Dựa trên yêu cầu Vertical Slice (chức năng theo chiều dọc) trong `AGENTS.md`:

**A. SQL Server:**
- **Schemas:** Phân tách schema rõ ràng: `auth`, `customer`, `payment`, `workflow`, `audit`.
- **Concurrency & Integrity:** Thêm cột `rowversion` cho các bảng cần thiết. Dùng các constraint chặt chẽ (Unique Filtered Index cho CCCD đang hoạt động).
- **Audit Logging:** Các bảng nghiệp vụ quan trọng nên có cơ chế sinh Audit log thông qua trigger hoặc tại Application layer (trong cùng transaction).

**B. Backend (API v2 - ASP.NET Core):**
- **Kiến trúc:** Vertical Slice Architecture kết hợp CQRS (có thể dùng MediatR) để cô lập các handler xử lý nghiệp vụ, tương ứng với `execution_handler_code`.
- **Authorization:** Tạo Custom Policy/Requirement trong ASP.NET Core để kiểm tra quyền hạn (`Effective Allow`), phạm vi dữ liệu (`COMPANY_ID`) và Ủy quyền (Delegation) trên từng endpoint.
- **Middleware:** Global Exception Handler trả về cấu trúc `ProblemDetails`. Middleware ghi nhận Correlation ID và Actor Identity.

**C. Frontend (React + TypeScript):**
- **Kiến trúc:** Feature-based routing & folder structure (nhóm component, hook, api call theo từng nghiệp vụ như Customer, Payment, Workflow).
- **Bảo mật:** Lưu trữ token an toàn. Tạo các HOC (Higher-Order Components) hoặc Hooks để kiểm tra quyền và tự động ẩn/hiện chức năng (dù backend vẫn chặn).
- **Validation:** Validation dữ liệu chặt chẽ ở client (Zod hoặc Yup) để giảm tải cho server trước khi gửi (đặc biệt khi cấu hình workflow động).

## 5. Cấu trúc repository đề xuất
```text
PTKD-ERP/
├── database/                   # Chứa script SQL
│   ├── migrations/             # Các script V1__...sql (Forward)
│   ├── rollbacks/              # Các script U1__...sql (Rollback)
│   └── seeds/                  # Idempotent seed data (roles, permissions, catalog)
├── docs/                       # Tài liệu (business, architecture)
├── src/
│   ├── backend/                # ASP.NET Core Web API
│   │   ├── PTKD.Api/           # Controllers, Middleware, Program.cs
│   │   ├── PTKD.Core/          # Domain Entities, Enums, Interfaces, Exceptions
│   │   └── PTKD.Features/      # Vertical Slices (Handlers, DTOs, Validators)
│   └── frontend/               # React + TypeScript
│       ├── public/
│       └── src/
│           ├── api/            # API Clients (Axios/Fetch)
│           ├── core/           # Auth, Context, Utils, Constants
│           ├── features/       # Workflow, Payment, Customer...
│           └── shared/         # Common UI Components
└── tests/
    ├── backend/                # Unit Tests & Integration Tests
    └── frontend/               # Unit Tests (Components/Hooks)
```

## 6. Các giai đoạn triển khai
Để đảm bảo giá trị bàn giao theo phương pháp Agile/Vertical Slice:
- **Phase 1: Foundation & Auth:** Thiết lập Repository (Scaffolding), CI/CD cục bộ. Thiết kế DB nền tảng. Xây dựng module Phân quyền (Department, Role, Individual, Permission Cache).
- **Phase 2: Customer Master:** API và UI cho quản lý khách hàng (Global), Customer_Company_Context (Company), chống trùng lặp, lịch sử thay đổi.
- **Phase 3: Dịch vụ & Thanh toán:** Flow thu tiền (CASHIER tự confirm), in bill, hiệu chỉnh bill đã thanh toán bởi ADMIN_PAYMENT kèm xử lý dây chuyền các kỳ đối soát.
- **Phase 4: Workflow Engine (Design-time):** Xây dựng module để Admin cấu hình Workflow (Steps, Approver Rules, Conditions, SLA/Reminders, Bindings).
- **Phase 5: Workflow Runtime & Execution:** Thực thi yêu cầu duyệt, Resolve Approvers, Xử lý Ủy quyền (Delegation), Maker-Checker, và gọi Execution Handlers sau khi duyệt xong.
- **Phase 6: Hoàn thiện Audit & Báo cáo:** Che dữ liệu nhạy cảm (Masking), hoàn thiện System Audit Logs, luồng Thông báo (Notifications).

## 7. Rủi ro và điểm mâu thuẫn trong tài liệu
- **Rủi ro Đồng thời (Concurrency) khi Duyệt:** Quy tắc *"Hai approver hợp lệ cùng nhìn thấy step, hành động đầu tiên thành công, hành động sau nhận CONFLICT"* đòi hỏi bắt buộc phải dùng `rowversion` và Isolation Level đủ nghiêm ngặt trên `Approval_Request_Steps`.
- **Rủi ro Hiệu chỉnh Payment:** Việc đổi `payment_date` và `company_id` trên payment đã CONFIRMED sẽ kích hoạt tính toán lại tối đa 4 kỳ đối soát (ngày/tháng cũ và mới). Đây là logic cực kỳ phức tạp và dễ gây deadlock/lệch số liệu nếu transaction chạy quá lâu.
- **Quy trình chưa rõ ràng:** Quy trình `SELL_CARE_PACKAGE` đang ở trạng thái `RESERVED / INACTIVE`. Cần tránh lập trình hay giả định schema cho nghiệp vụ này cho đến khi có đặc tả chính thức.
- **Nút thắt cổ chai ở Delegation:** Ủy quyền (Delegation) bắt buộc phải do Admin kích hoạt (`DELEGATION_ACTIVATE`). Nếu Admin phản hồi chậm, SLA của yêu cầu đang chờ phê duyệt sẽ bị vi phạm.
- **Định danh điện thoại:** Điện thoại không phải là Unique Key tuyệt đối (chỉ dùng làm cảnh báo trùng). Backend cần cho phép lưu trùng số điện thoại mà không bị văng lỗi cơ sở dữ liệu (Database Constraint).
- **Mâu thuẫn ngầm:** Tài liệu yêu cầu *Không lưu mật khẩu, token trong Git*, nhưng chưa chỉ định rõ cơ chế quản lý secret môi trường dev (ví dụ: User Secrets, .env file).
