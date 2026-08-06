# PTKD-ERP — Chỉ mục quyết định (DECISION-INDEX)

> **Tài liệu này là BẢN ĐỒ, không phải nguồn sự thật.** Nội dung quyết định nằm ở file gốc được trỏ tới ở cột cuối. Không sao chép nội dung vào đây; khi quyết định thay đổi, sửa ở file gốc rồi cập nhật một dòng ở bảng này.
>
> Người soạn: **Đào Hải Bách** · Đơn vị: **Phòng CNTT** · Ngày lập: 06/08/2026
> Ảnh chụp tại: tag `phase-1b10-release-readiness-v1.0`, nhánh `feature/phase-1-organization`
> Phạm vi quét: 397 file trong `docs/architecture/`, `docs/reviews/`, `docs/decisions/`

---

## Vì sao có tài liệu này

Dự án đã ra **118 quyết định có mã** cộng thêm khoảng 20 quyết định không mã, trải trên 397 file. Riêng nhóm `DEC-1B` có sổ đăng ký tập trung; **8 nhóm còn lại thì không** — chúng nằm lẫn giữa hàng trăm phiếu nghiệm thu thủ tục.

Hệ quả thực tế: muốn biết *"vì sao endpoint `/auth/me/permissions` không trả quyền COMPANY khi thiếu header?"* thì phải đoán đúng tên file trong 397 file. Tài liệu này giải quyết đúng việc đó.

**Cách dùng:** tra ở bảng → mở file gốc → đọc chi tiết. Đừng đọc tuần tự.

---

## 1. Bản đồ nhóm quyết định

| Nhóm mã | Số lượng | Chủ đề | Sổ đăng ký tập trung? | File gốc |
|---|---|---|---|---|
| `DEC-1B-001…021` | 21 | Nền tảng xác thực & phân quyền | ✅ **Có** | `docs/decisions/phase-1b0-open-decisions.md` |
| `OD-D-01…08` | 8 | Bộ tính quyền hiệu dụng (Permission Evaluator) | ❌ | `phase-1b1d-permission-evaluation-implementation-plan.md` |
| `OD-E-01…08` | 8 | Cưỡng chế quyền & ngữ cảnh công ty ở API | ❌ | `phase-1b1e-company-context-permission-enforcement-plan.md` |
| `OD-F-01…12` | 12 | Ghi audit & khởi tạo admin đầu tiên | ❌ | `phase-1b1f-audit-writer-bootstrap-plan.md` |
| `OD-1B6-001…010` | 10 | Module Dịch vụ | ❌ | `phase-1b6b-project-owner-backend-data-scope-acceptance.md` |
| `OD-1B7-001…020` | 20 | Module Thanh toán & đối soát | ❌ | Hỏi: `phase-1b7-payment-foundation-discovery-and-detailed-plan.md`<br>Đáp: `phase-1b7-project-owner-scope-acceptance.md` |
| `OD-1B8-001…015` | 15 | Module In lại thẻ | ❌ | Hỏi: `phase-1b8-card-reprint-discovery-and-scope-plan.md`<br>Đáp: `phase-1b8a-project-owner-blocker-decision-response.md` |
| `B1-BLOCKER-001…006` | 6 | Gỡ chặn triển khai In lại thẻ (vòng 2) | ❌ | `phase-1b8b1-project-owner-blocker-decision-response.md` |
| `OD-1B9-001…018` | 18 | Module Bán gói chăm sóc | ❌ | `phase-1b9a-project-owner-blocker-decision-response.md` |
| *(không mã)* | ~20 | Chặn giữa chừng, chọn việc kế tiếp, phát hành | ❌ | Xem §4 và §5 |

**Lưu ý cấu trúc:** nhóm `OD-1B7` và `OD-1B8` tách **câu hỏi** và **câu trả lời** ra hai file khác nhau. File `discovery-and-scope-plan` chỉ nêu vấn đề còn ngỏ; muốn biết đã chốt gì phải mở file `scope-acceptance` / `blocker-decision-response`.

---

## 2. Quyết định nền tảng — `DEC-1B` (21 mã)

Đã có sổ tập trung, **không lặp lại ở đây**. Mở `docs/decisions/phase-1b0-open-decisions.md`.

Duyệt bởi Đào Hải Bách ngày 15/07/2026 theo Mô hình Quản trị Một Chủ (Single-Owner Governance Model). Trạng thái: 19 mã APPROVED WITH CONDITIONS · `DEC-1B-008` MERGED vào `DEC-1B-007` · `DEC-1B-017` DEFERRED.

Sáu mã hay phải tra nhất:

| Mã | Chốt gì |
|---|---|
| `DEC-1B-003` | Access token 15 phút · Refresh token 7 ngày · sai lệch đồng hồ 30s |
| `DEC-1B-005` | Refresh token dùng một lần, chỉ lưu hash, tái sử dụng → thu hồi cả họ token, **không** có thời gian ân hạn |
| `DEC-1B-011` | Cache quyền lỗi → **fail closed**, trả HTTP 503. Tuyệt đối không dùng quyền cũ |
| `DEC-1B-012` | Thiếu `X-Company-Id` → 400 · Không có quyền → 403 · JWT **không** cấp quyền công ty |
| `DEC-1B-015` | Audit bất biến bằng **INSTEAD OF trigger** ở tầng DB, chặn UPDATE/DELETE/TRUNCATE |
| `DEC-1B-020` | Tài khoản bị khóa → HTTP **403**. Phản hồi ra ngoài không được để lộ tài khoản có tồn tại hay không |

---

## 3. Quyết định kỹ thuật theo tầng

### 3.1 `OD-D` — Bộ tính quyền hiệu dụng (8 mã)

📄 `docs/architecture/phase-1b1d-permission-evaluation-implementation-plan.md`

| Mã | Chốt gì |
|---|---|
| `OD-D-01` | **DENY cá nhân luôn thắng** mọi grant từ Admin Group |
| `OD-D-02` | Nhiều phòng ban đang hoạt động → lấy **hợp** (union) quyền của tất cả |
| `OD-D-03` | Không có phân công vào công ty được hỏi → trả **DENY** |
| `OD-D-04` | Dùng `IMemoryCache`. Cache phân tán để sau |
| `OD-D-05` | `GET /api/v2/security/permissions` chỉ trả quyền đang active |
| `OD-D-06` | API quyền hiệu dụng **bắt buộc** có company scope tường minh khi đánh giá quyền COMPANY |
| `OD-D-07` | Dùng/gán quyền đã inactive → HTTP **422** |
| `OD-D-08` | Test tích hợp dùng DB `PTKD_TEST_PHASE1A2` |

### 3.2 `OD-E` — Cưỡng chế quyền ở API (8 mã)

📄 `docs/architecture/phase-1b1e-company-context-permission-enforcement-plan.md`

| Mã | Chốt gì |
|---|---|
| `OD-E-01` | Dùng **attribute trên endpoint** `[RequirePermission("CODE", Scope=…)]` + một filter dùng chung. **Không** dùng registry route–permission tập trung |
| `OD-E-02` | Chỉ endpoint đánh dấu COMPANY-scoped mới đòi `X-Company-Id`. Không bắt toàn bộ |
| `OD-E-03` | API Security Administration (nhóm D-B) **giữ nguyên** cưỡng chế thủ công, chưa chuyển sang cơ chế chung |
| `OD-E-04` | Endpoint GLOBAL: `X-Company-Id` là tùy chọn và **bị bỏ qua**, không từ chối |
| `OD-E-05` | Giai đoạn E-A chỉ dựng nền + endpoint test. Không áp rộng ra API cũ |
| `OD-E-06` | `X-Company-Id` sai/thiếu → **400** kèm mã lỗi đã khử thông tin nội bộ |
| `OD-E-07` | Metadata quyền dùng attribute strongly-typed |
| `OD-E-08` | **Một endpoint = đúng một quyền.** Any-of/all-of hoãn, cần quyết định riêng |

### 3.3 `OD-F` — Audit & khởi tạo admin (12 mã)

📄 `docs/architecture/phase-1b1f-audit-writer-bootstrap-plan.md`

| Mã | Chủ đề |
|---|---|
| `OD-F-01` | `SecurityAuditEvent`: domain entity hay write record |
| `OD-F-02` | Hợp đồng và vị trí interface `IAuditWriter` |
| `OD-F-03` | Chiến lược truy cập dữ liệu của audit writer (`PTKD.Infrastructure` ServiceCollection extension) |
| `OD-F-04` | ⭐ **Fail-closed.** Không có thay đổi mật khẩu nào tồn tại được nếu thiếu bản ghi audit `PASSWORD_CHANGED`. `IAuditWriter.WriteAsync` ném lỗi → nghiệp vụ chính rollback |
| `OD-F-05` | Ánh xạ entity `SecurityBootstrapState` |
| `OD-F-06` | Cơ chế truyền lệnh bootstrap |
| `OD-F-07` | Cơ chế nhập bí mật bootstrap (không in ra console, không ghi log) |
| `OD-F-08` | Mã sự kiện audit cho bootstrap |
| `OD-F-09` | DB dùng cho test tích hợp Phase F |
| `OD-F-10` | Thứ tự các phase con |
| `OD-F-11` | Role `PTKD_Security_Audit_Runtime` cho runtime ứng dụng |
| `OD-F-12` | Danh mục loại trừ tường minh của Phase F |

> `OD-F-04` là quyết định có ảnh hưởng rộng nhất nhóm này — nó là lý do tồn tại của `SqlTransactionalAuditWriter.cs`.

---

## 4. Quyết định theo module nghiệp vụ

### 4.1 `OD-1B6` — Dịch vụ (10 mã)
📄 `phase-1b6b-project-owner-backend-data-scope-acceptance.md`

Phân loại loại dịch vụ · trạng thái vòng đời · mô hình gia hạn · phạm vi giá chuẩn · bán dịch vụ trong 1B.6 · quyền `SERVICE_VIEW` · liên kết dịch vụ–khách hàng · endpoint quản trị loại dịch vụ · phạm vi migration · phạm vi màn hình frontend.

### 4.2 `OD-1B7` — Thanh toán & đối soát (20 mã)
📄 Hỏi: `phase-1b7-payment-foundation-discovery-and-detailed-plan.md` → Đáp: `phase-1b7-project-owner-scope-acceptance.md`

Nhóm nặng nhất về tài chính. Các mã đáng chú ý:

| Mã | Vấn đề |
|---|---|
| `OD-1B7-002` | Định dạng sinh mã hóa đơn (tiền tố / chuỗi / theo ngày) |
| `OD-1B7-004` | Cơ chế chặn thanh toán trùng cùng một chu kỳ dịch vụ |
| `OD-1B7-006` | Chính xác những trường Admin được sửa **sau khi** đã xác nhận |
| `OD-1B7-014` | `payment_date` là ngày **nhận tiền** hay ngày **nhập liệu** |
| `OD-1B7-018` | In lại thẻ / Gói chăm sóc có dùng chung bảng `Payment_Transaction` không |
| `OD-1B7-020` | `total_amount` là `decimal(18,0)` hay `decimal(18,2)` cho VND |

### 4.3 `OD-1B8` + `B1-BLOCKER` — In lại thẻ (15 + 6 mã)
📄 Hỏi: `phase-1b8-card-reprint-discovery-and-scope-plan.md`
📄 Đáp: `phase-1b8a-project-owner-blocker-decision-response.md`
📄 Gỡ chặn vòng 2: `phase-1b8b1-project-owner-blocker-decision-response.md`

Bốn quyết định định hình module:

- `OD-1B8-001` — Hai thuật ngữ: **Initial Print** (lần in đầu) và **Reprint** (mọi lần sau). Hệ thống phải đếm được số lần in.
- `OD-1B8-004` — Phí in lại **50.000 VND**/thẻ, nhưng **phải cấu hình được** theo mẫu giá dịch vụ có hiệu lực theo ngày. Cấm hard-code trong mã nguồn.
- `OD-1B8-005` — Trình tự bắt buộc: tạo yêu cầu → duyệt (nếu cần) → sinh hóa đơn → **thanh toán CONFIRMED rồi mới được in/giao thẻ**.
- `OD-1B8-006` — MVP chỉ theo dõi **trạng thái** vật lý. Không quản lý tồn kho phôi/con dấu.

Nhóm `B1-BLOCKER` ghi lại một lần triển khai bị dừng đúng lúc: kế hoạch định tạo project mới `PTKD.CardReprint`, bị bác — buộc dùng cấu trúc phân tầng sẵn có (`B1-BLOCKER-001`), và tách rõ ranh giới B1 (dữ liệu/API nền) với B2 (tích hợp workflow + thanh toán) (`B1-BLOCKER-002`).

### 4.4 `OD-1B9` — Bán gói chăm sóc (18 mã)
📄 `phase-1b9a-project-owner-blocker-decision-response.md`

Thuật ngữ · đơn vị bán · thời hạn gói · nguồn giá · cách tính giá · xử lý đổi giá · quy tắc gia hạn · điều kiện kích hoạt duyệt · chiết khấu · thời điểm thanh toán · ràng buộc thanh toán · đối soát/báo cáo · quyền · phạm vi frontend · mô hình dữ liệu · migration · tiêu chí nghiệm thu · ranh giới ngoài phạm vi.

> ⚠️ Module này đã có bảng và execution handler nhưng `SELL_CARE_PACKAGE` vẫn ở trạng thái **RESERVED/INACTIVE** trong `Business_Process_Catalog`.

---

## 5. Quyết định không mã — vẫn quan trọng

Những quyết định này **không có mã định danh**, dễ thất lạc nhất. Đây chính là lý do chỉ mục cần tồn tại.

| Chủ đề | Chốt gì | File gốc |
|---|---|---|
| **Hợp đồng API quyền hiện tại** ⭐ | `GET /api/v2/auth/me/permissions` **không** trả quyền COMPANY nếu thiếu `X-Company-Id`. Từ chối redesign `PermissionEvaluator`. Gating ở frontend chỉ mang tính tham khảo — backend luôn là nơi quyết định | `phase-1b1l-company-scope-blocker-decision.md` |
| **Bảo vệ SystemController** | `GET /api/v2/system/info` phải gắn `SECURITY_ADMIN_MANAGE` scope GLOBAL vì nó lộ `ASPNETCORE_ENVIRONMENT`. Tái dùng quyền cũ thay vì tạo mã quyền mới | `phase-1b1e-post-completion-owner-decisions.md` |
| **Thiếu seed quyền — dừng cứng** | Phát hiện `SECURITY_ADMIN_MANAGE` chưa được seed, dừng triển khai để vá ngược | `phase-1b1f-b-hard-stop-security-admin-manage-seed-gap.md` |
| **Môi trường diễn tập solo** | Chấp nhận SQL Server non-production với dữ liệu tổng hợp thay cho snapshot production đã khử nhạy cảm, **có ghi rõ hạn chế độ trung thực**. Chỉ 3 DB được phép reset/drop | `phase-1b10c-project-owner-solo-environment-decision.md` |
| **Sửa sổ theo dõi migration** | `SchemaVersions` được chỉnh lại thành 15 dòng V0001–V0015, backup trước khi sửa tại `C:\temp\PTKD_PROD_pre_tracking_correction.bak` | `phase-1b10c-project-owner-correction-decision-response.md` |
| **17 quyết định thực thi migration** | Bộ quyết định cho phép chạy migration lên `PTKD_PROD` | `phase-1b10d-production-migration-execution-authorization.md` |
| **Đích push** | Remote duy nhất được phép: `origin` → `https://github.com/cugiadixe/1prj`. Cấm force push, cấm `--tags`, cấm `--all` | `phase-1b10g-project-owner-push-destination-decision.md` |
| **Mô hình quản trị** | Single-Owner Governance Model — một người vừa là Project Owner, BA, DBA, Security, QA | `reviews/phase-1b0-single-owner-governance-proposal.md` |

### Chuỗi quyết định chọn việc kế tiếp

Sau mỗi module, có một cặp file *khuyến nghị → quyết định*. Đọc chuỗi này là dựng lại được **vì sao dự án đi theo thứ tự đó**:

`phase-1b2-next-work-selection-review` → `phase-1b3-post-b4` → `post-1b4` → `post-1b5` → `post-1b6` → `post-1b7` → `post-phase-1b8` → `post-phase-1b9`

Quyết định định hình toàn bộ lộ trình: **dựng bộ máy workflow động trước, module nghiệp vụ sau** — sửa lại giả định ban đầu là làm Customer Master trước (xem `implementation-roadmap-v1.0.md`).

---

## 6. Quyết định kỹ thuật tổng thể

📄 `docs/architecture/technical-decisions-v1.0.md` — ngắn, nên đọc trọn vẹn.

Những lựa chọn **cố ý loại bỏ**, hay bị hỏi lại nhất:

| Đã chọn | Đã loại | Ghi ở |
|---|---|---|
| Modular Monolith, Vertical Slice | Microservices | `technical-decisions-v1.0.md` |
| Gọi service trực tiếp | MediatR | nt |
| Script SQL đánh số thủ công + rollback 1-1 | EF Core Migrations | nt |
| EF Core cho CRUD, Dapper/stored proc cho giao dịch nhạy cảm | EF Core cho tất cả | nt |
| Serilog (log kỹ thuật) tách hẳn audit nghiệp vụ (SQL) | Gộp chung một đường | nt |
| JWT nội bộ, để ngỏ abstraction cho AD/LDAP | Tích hợp AD ngay từ đầu | nt |

---

## 7. Cách phân biệt file quyết định với file thủ tục

Trong 397 file, chỉ **67 file chứa quyết định thật**; 330 file còn lại là thủ tục nghiệm thu. Quy tắc nhận dạng theo tên file:

**Chứa quyết định — đáng mở:**
`*-decision.md` · `*-decision-response.md` · `*-blocker-*.md` · `*-open-decisions*.md` · `*-discovery-and-*-plan.md` · `*-scope-acceptance.md` · `*-next-work-*decision.md` · `technical-decisions-*.md` · `implementation-roadmap-*.md`

**Chỉ là thủ tục — bỏ qua khi tra cứu:**
`*-project-owner-plan-acceptance.md` · `*-project-owner-implementation-acceptance.md` · `*-project-owner-final-acceptance.md` · `*-final-closure-review.md` · `*-acceptance-review.md` · `*-implementation-report.md` · `*-authorization.md`

Lệnh lọc nhanh:

```bash
ls docs/architecture/ | grep -E "decision|blocker|discovery-and|scope-acceptance|roadmap"
```

---

## 8. Việc còn để ngỏ

1. **Tám nhóm mã chưa có sổ đăng ký.** Chỉ `DEC-1B` có. Cân nhắc gộp `OD-D`/`OD-E`/`OD-F` thành một sổ `docs/decisions/phase-1b1-open-decisions.md` theo đúng khuôn của `phase-1b0`.
2. **Mã không liên tục.** Nhóm `OD-F` nhảy — `OD-F-01` định nghĩa ở file kế hoạch nhưng `OD-F-04` lại được trích dẫn ở `phase-1b1g-final-closure-review.md`. Nên xác nhận file nào là bản có thẩm quyền cho từng mã.
3. **~20 quyết định vẫn không mã** (§5). Nên cấp mã hồi tố, ví dụ tiền tố `DEC-REL-` cho nhóm phát hành 1B.10.
4. **`OD-1B7` tách hỏi–đáp hai file.** Người đọc dễ dừng ở file câu hỏi rồi tưởng chưa chốt. Nên thêm dòng trỏ chéo ở đầu file discovery.
5. **Chỉ mục này phải cập nhật thủ công.** Mỗi quyết định mới thêm một dòng. Nếu bỏ bê, nó sẽ tệ hơn không có — vì tạo cảm giác an toàn giả.

---

## 9. Cho AI đọc repo này

Thứ tự nạp ngữ cảnh cho một câu hỏi kiểu *"vì sao hệ thống làm X theo cách này?"*:

```
1. AGENTS.md                      → luật bắt buộc
2. ARCHITECTURE.md                → định vị
3. DECISION-INDEX.md (file này)   → tra xem quyết định nằm ở đâu
4. Mở đúng 1–2 file gốc           → đọc chi tiết
```

**Đừng** grep mù trong `docs/architecture/`. 330/397 file là thủ tục nghiệm thu, sẽ nhiễu kết quả và ăn hết cửa sổ ngữ cảnh.

**Nếu không tìm thấy quyết định trong chỉ mục:** báo là chưa tìm thấy, **không suy diễn**. Đây là luật ghi rõ ở `AGENTS.md` — thiếu, mâu thuẫn hay mơ hồ thì dừng và báo, không tự mở rộng yêu cầu.
