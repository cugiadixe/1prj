# PTKD-ERP — Bản đồ kiến trúc

> **Mục đích của tài liệu này:** cho một người mới hoặc một AI đọc trong ~10 phút là định vị được dự án, biết code nằm ở đâu, và biết đọc tiếp file nào. Đây là **bản đồ**, không phải đặc tả. Đặc tả nghiệp vụ nằm ở `docs/business/PTKD-Specification-v1.1.md`.
>
> Người soạn: **Đào Hải Bách** · Đơn vị: **Phòng CNTT** · Ngày: 06/08/2026
> Ảnh chụp tại: tag `phase-1b10-release-readiness-v1.0`, nhánh `feature/phase-1-organization`

---

## 1. Dự án này là gì

Hệ thống ERP nội bộ của INDEVCO cho nghiệp vụ **PTKD** (Phòng/Phát triển Kinh doanh) — quản lý khách hàng, dịch vụ chăm sóc, thanh toán, và thẻ, với một **bộ máy phê duyệt động (dynamic approval workflow)** làm xương sống.

Điểm khác biệt cốt lõi so với ERP thông thường: **quy trình phê duyệt không hard-code**. Quản trị viên cấu hình workflow qua giao diện (bước, người duyệt, điều kiện, ràng buộc theo công ty), hệ thống chạy theo cấu hình đó. Mọi nghiệp vụ nhạy cảm — tạo khách hàng, gộp trùng, đổi giá, in lại thẻ, xuất dữ liệu — đều đi qua bộ máy này.

Ứng dụng web nội bộ. Không public internet. Đa công ty (`company_id` là chiều phân quyền xuyên suốt).

---

## 2. Nền tảng kỹ thuật

| Lớp | Công nghệ |
|---|---|
| Backend | ASP.NET Core Web API, **.NET 10**, C# |
| Frontend | React 19 + TypeScript, Vite, **Ant Design 6** |
| State | TanStack Query (server state) · React Hook Form + Zod (form) |
| Database | Microsoft SQL Server (`PTKD_DEV` / `PTKD_TEST` / `PTKD_PROD`) |
| ORM | EF Core cho CRUD; Dapper/stored proc cho giao dịch phức tạp |
| Xác thực | JWT nội bộ + cookie + CSRF token (để ngỏ đường tích hợp AD/LDAP) |
| Test | xUnit + NSubstitute + WebApplicationFactory · Vitest + RTL |
| Migration | Script SQL đánh số thủ công, **không** dùng EF Migrations |

**Kiến trúc:** Modular Monolith, tổ chức theo Vertical Slice (cắt dọc theo tính năng nghiệp vụ). Cố ý **không** dùng microservices, **không** dùng MediatR.

**Cổng vào:** API `http://localhost:5057` (Swagger tại `/swagger`) · UI `http://localhost:5173` · Mọi endpoint công khai đều có tiền tố **`/api/v2`**.

---

## 3. Bản đồ thư mục — đọc gì ở đâu

```
PTKD-ERP/
├── AGENTS.md              ★ LUẬT BẮT BUỘC cho AI làm việc trên repo. ĐỌC ĐẦU TIÊN.
├── README.md                Cách chạy backend/frontend/test/migration
├── CHANGELOG.md
│
├── docs/
│   ├── business/          ★ NGUỒN SỰ THẬT NGHIỆP VỤ
│   │   ├── PTKD-Specification-v1.1.md    Đặc tả gốc (~109K ký tự)
│   │   ├── business-rules.md
│   │   ├── permission-catalog.md          56 quyền
│   │   ├── process-catalog.md             8 quy trình nghiệp vụ (bảng chuẩn)
│   │   ├── approval-workflow-rules.md     Bất biến của bộ máy duyệt
│   │   └── acceptance-criteria.md
│   ├── architecture/
│   │   ├── technical-decisions-v1.0.md   ★ Quyết định kỹ thuật đã chốt
│   │   ├── implementation-roadmap-v1.0.md ★ Lộ trình Phase 0 → 6
│   │   └── phase-1b*-*.md                 ⚠ ~400 file nhật ký phê duyệt.
│   │                                        KHÔNG đọc tuần tự. Chỉ tra khi
│   │                                        cần biết "vì sao hồi đó quyết vậy".
│   ├── decisions/         (rỗng — chỗ dành cho ADR, xem §7)
│   └── reviews/           Biên bản review BA/DBA/Security giai đoạn 1B.0
│
├── database/
│   ├── migrations/        V0001 → V0015 (tiến)
│   └── rollbacks/         U0001 → U0015 (lùi, 1-1 với migration)
│
├── src/backend/           PTKD-ERP.sln
│   ├── PTKD.Domain/       Entity + quy tắc nghiệp vụ thuần (38 entity)
│   ├── PTKD.Application/  Service, DTO, Validator, ExecutionHandler
│   ├── PTKD.Infrastructure/ EF Core config, DbContext, audit ghi SQL
│   ├── PTKD.Api/          29 Controller, ~155 endpoint, filter, JWT/CSRF
│   ├── PTKD.DbMigrator/   Chạy script SQL (có --dry-run)
│   ├── PTKD.Bootstrap/    Khởi tạo admin đầu tiên
│   └── PTKD.Worker/       Background service
│
├── src/frontend/src/      66 trang React, gom theo module nghiệp vụ
│
├── tests/backend/
│   ├── PTKD.UnitTests/        Quy tắc nghiệp vụ
│   ├── PTKD.IntegrationTests/ Schema, transaction, concurrency thật trên SQL
│   └── PTKD.ApiTests/         Phân quyền, validation, mã lỗi
│
└── scripts/               build-all · run-backend · run-frontend · test-all (PowerShell)
```

---

## 4. Bốn trụ cột của hệ thống

### 4.1 Phân quyền (Security / Authorization)

Quyền hiệu dụng của một người = tổng hợp từ **bốn nguồn**, có luật deny:

1. Quyền theo **phòng ban** (`DepartmentPermission`)
2. Quyền theo **vai trò** (`Role` → `RolePermission`)
3. Quyền theo **nhóm quản trị** (`AdminGroup` → `AdminGroupPermission`)
4. Quyền **cá nhân** (`UserIndividualPermission`) — có thể GRANT hoặc DENY

Bộ tính quyền: `PTKD.Application/Security/Authorization/Services/PermissionEvaluator.cs`
Chặn ở API: `PTKD.Api/Security/Authorization/PermissionAuthorizationFilter.cs` + attribute `[RequirePermission]`

> **Luật cứng:** phân quyền và phạm vi `company_id` **phải** cưỡng chế ở backend. Ẩn/hiện ở frontend không phải biện pháp bảo mật.

### 4.2 Bộ máy phê duyệt động (Workflow)

Tách làm hai nửa:

**Design-time** — quản trị viên cấu hình:
`WorkflowDefinition` → `WorkflowDefinitionVersion` → `WorkflowStep` → `WorkflowStepApproverRule` / `WorkflowCondition`, gắn vào quy trình qua `WorkflowBinding`.

**Runtime** — khi có yêu cầu thật:
`WorkflowInstance` → `WorkflowInstanceStep` → `WorkflowInstanceStepAssignee`, ghi hành động vào `WorkflowAction`.

Điều phối: `PTKD.Application/Workflows/Services/WorkflowRuntimeService.cs` (~39K ký tự — file lõi nhất dự án).
Tìm người duyệt: `ApproverResolver.cs`.
Thi hành sau khi duyệt xong: `WorkflowExecutionHandlerFactory.cs` chọn handler theo `process_code`.

**Bất biến phải nhớ** (chi tiết ở `docs/business/approval-workflow-rules.md`):
- Các bước duyệt chạy **tuần tự**, mỗi lúc chỉ một bước active.
- Người gửi **không bao giờ** được duyệt yêu cầu của chính mình, kể cả qua ủy quyền.
- Không tìm được người duyệt → **chặn**, báo người gửi. Tuyệt đối không tự đẩy sang admin.
- Phiên bản workflow đã publish là **bất biến**. Yêu cầu đang chạy giữ nguyên phiên bản gốc.
- Binding theo công ty **thắng** binding toàn cục.
- Quá hạn chỉ được **nhắc**, cấm tự duyệt / tự từ chối / tự chuyển.
- Bản ghi hành động và audit là **append-only**.

### 4.3 Tám quy trình nghiệp vụ

| `process_code` | Tên tiếng Việt | Module | Chế độ duyệt |
|---|---|---|---|
| `CREATE_CUSTOMER` | Tạo khách hàng mới | CUSTOMER | Bắt buộc |
| `CUSTOMER_MASTER_CHANGE` | Đổi dữ liệu KH dùng chung | CUSTOMER | Bắt buộc |
| `CUSTOMER_MERGE_DUPLICATE` | Gộp khách hàng trùng | CUSTOMER | Bắt buộc/theo policy |
| `CHANGE_OWNER` | Thay đổi chủ sở hữu | PLOT | Bắt buộc |
| `SERVICE_PRICE_OVERRIDE` | Áp giá khác giá chuẩn | SERVICE | Có điều kiện |
| `CARD_REPRINT` | In lại thẻ mộ | CARD | Có điều kiện (từ lần in thứ 2) |
| `IMPORT_ROLLBACK` | Hoàn tác import | IMPORT | Bắt buộc/theo policy |
| `SENSITIVE_EXPORT` | Xuất dữ liệu nhạy cảm | EXPORT | Bắt buộc/theo policy |

Thêm `SELL_CARE_PACKAGE` (Bán gói chăm sóc) — đã có bảng và handler nhưng trạng thái **RESERVED/INACTIVE**, chờ đặc tả module bán dịch vụ.

> Quản trị viên **chỉ** cấu hình được workflow cho quy trình đã do lập trình đăng ký sẵn. Không tự sinh quy trình mới từ giao diện.

### 4.4 Audit nghiệp vụ

Tách bạch hoàn toàn với log kỹ thuật (Serilog). Audit nghiệp vụ ghi vào SQL Server, **append-only**, chặn sửa bằng `AppendOnlyInterceptor.cs`.

Ghi: `SqlSecurityAuditWriter.cs` / `SqlTransactionalAuditWriter.cs` (bản transactional dùng khi cần audit và nghiệp vụ cùng sống chết trong một transaction).
Lọc dữ liệu nhạy cảm trước khi ghi: `SecurityAuditEventRecord.cs` — có regex chặn `password|token|secret|signing_key|private_key|api_key` lọt vào JSON audit.

---

## 5. Luồng dữ liệu điển hình

Ví dụ: nhân viên đề xuất tạo khách hàng mới.

```
[UI] CustomerProposalCreatePage.tsx
      │  POST /api/v2/... (kèm CSRF token, JWT cookie)
      ▼
[API] CustomerProposalController
      │  ① ValidationFilter → ② PermissionAuthorizationFilter (quyền + company_id)
      ▼
[App] CustomerProposalService
      │  tạo CustomerChangeRequest
      ▼
[Workflow] WorkflowRuntimeService.Start(process_code = CREATE_CUSTOMER)
      │  ├─ WorkflowBinding: tìm binding công ty trước, rồi binding toàn cục
      │  ├─ chụp snapshot WorkflowDefinitionVersion (bất biến)
      │  └─ ApproverResolver → sinh WorkflowInstanceStepAssignee
      ▼
[Người duyệt] WorkflowMyApprovalsPage.tsx → Approve
      │  (trong 1 transaction: ghi WorkflowAction + audit + chuyển bước)
      ▼
[Bước cuối duyệt xong]
[Handler] WorkflowExecutionHandlerFactory → CreateCustomerExecutionHandler
      │  tạo Profile + Customer + CustomerCompanyContext
      ▼
[Audit] SqlTransactionalAuditWriter ghi actor · entity · company · before/after · correlation_id
```

Điểm nghẽn cần chú ý khi sửa: bước duyệt dùng `rowversion` (optimistic concurrency) + `DeadlockRetryPolicy.cs`. Hai người duyệt cùng lúc là kịch bản đã có test.

---

## 6. Quy ước bất di bất dịch

Trích từ `AGENTS.md` — ai (người hay AI) sửa repo này đều phải tuân:

**Kiến trúc & API**
- Mọi endpoint dùng tiền tố `/api/v2`.
- Không phơi entity ra API. Luôn qua DTO.
- Lỗi trả theo ProblemDetails + mã lỗi nghiệp vụ ổn định.
- Người thực hiện lấy từ ngữ cảnh đã xác thực. **Không tin** actor/creator/approver ID do client gửi lên.
- Không hard-code ID người duyệt trong mã nguồn.
- Không build điều kiện workflow do admin nhập thành SQL/C#/JS thô (chống injection).

**Database**
- Mỗi migration tiến **phải** có script lùi tương ứng.
- Seed phải idempotent.
- Không tự áp schema lúc khởi động ứng dụng.
- Không xóa/ghi đè lịch sử audit.

**Bảo mật**
- Không đưa secret, password, token, connection string vào Git.
- Che dữ liệu khách hàng nhạy cảm trừ khi quyền hiệu dụng cho phép.

**Phạm vi**
- Không refactor ngoài phạm vi task.
- Không tự tạo quy trình nghiệp vụ / handler mới khi chưa có yêu cầu được duyệt.
- Không làm yếu hoặc xóa test cũ chỉ để code mới chạy qua.

---

## 7. Trạng thái hiện tại và những gì còn thiếu

**Đã hoàn thành:** Phase 0 → Phase 1B.10. Nền tảng tổ chức, phân quyền, bộ máy workflow, module Khách hàng (tạo/sửa/gộp), Dịch vụ, Thanh toán & đối soát, In lại thẻ, Gói chăm sóc. Đã migrate `PTKD_PROD` (52 bảng, 56 quyền), gắn tag `phase-1b10-release-readiness-v1.0`, đã push GitHub.

**Chưa có:**
- Chưa merge về `main` — vẫn ở nhánh `feature/phase-1-organization`.
- Chưa có CI/CD, chưa cấu hình IIS, chưa deploy production thật.
- Chưa tích hợp AD/LDAP (đã để sẵn abstraction).
- `docs/decisions/` **rỗng** — chưa có ADR nào. Toàn bộ lý do quyết định đang nằm rải trong ~400 file phase, rất khó tra.
- `SELL_CARE_PACKAGE` còn INACTIVE.
- Chưa có E2E Playwright (đã quyết định dùng nhưng chưa viết).

---

## 8. Hướng dẫn cho AI đọc repo này

Nếu bạn là một mô hình AI được giao việc trên repo này, đọc theo thứ tự sau — **đừng** nạp cả repo:

1. `AGENTS.md` — luật bắt buộc. Không đọc là làm sai.
2. Tài liệu này (`ARCHITECTURE.md`) — định vị.
3. `docs/business/` — file liên quan trực tiếp tới task.
4. Chỉ khi cần: mở đúng file mã nguồn theo bản đồ ở §3.

**Cảnh báo cửa sổ ngữ cảnh:** repo đầy đủ ~1,6 triệu token, vượt xa giới hạn mọi mô hình hiện tại. Đừng nạp `repomix-output.xml` nguyên khối. Nếu cần bản nén, lọc trước:

```bash
npx repomix --ignore "docs/architecture/phase-*,docs/reviews/**,**/*.test.tsx,tests/**"
```

Cách này bỏ nhật ký phê duyệt và test, giữ lại mã nguồn + đặc tả nghiệp vụ — xuống còn khoảng một phần năm.

**Nguyên tắc khi thiếu thông tin:** đặc tả nghiệp vụ thiếu, mâu thuẫn, hoặc mơ hồ thì **dừng và báo**, không tự suy diễn rồi mở rộng yêu cầu. Đây là luật ghi rõ trong `AGENTS.md`.
