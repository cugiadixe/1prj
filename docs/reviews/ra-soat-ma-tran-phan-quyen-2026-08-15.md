# Rà soát ma trận phân quyền & đề xuất thiết kế lại

> **Loại tài liệu:** BẢN NHÁP THẢO LUẬN. Không phải hồ sơ trình duyệt.
> **Ngày:** 2026-08-15 · **Người soạn:** AI (Claude) · **Yêu cầu bởi:** anh Bách
> **Phạm vi:** 5 mặt cắt (danh mục quyền · engine đánh giá · controller · frontend · ngữ cảnh công ty),
> mỗi mặt cắt có một vòng kiểm chứng đối kháng riêng. 80 phát hiện, 79 sống sót sau kiểm chứng, 1 bị bác.
> Mọi khẳng định dưới đây đều có `file:line` kèm theo.

---

## 1. KẾT LUẬN TRƯỚC

**Bốn ý đồ của anh không thể thực hiện bằng cách chỉnh cấu hình. Chúng bị chặn ở tầng mô hình.**

Lý do gọn trong một câu: **cột quyết định "quyền này có bị ràng theo công ty hay không" nằm ở
DÒNG DANH MỤC QUYỀN, không nằm ở Ô TRONG MA TRẬN.** Người quản trị không sửa được nó qua giao diện,
và 43/67 mã quyền đang khai giá trị khiến ngữ cảnh công ty bị **chặn cứng**.

Hệ quả cụ thể, đã đọc code xác nhận:

- Cấp `CUSTOMER_VIEW_BASIC` cho một người ở phạm vi "chỉ công ty A" → hệ thống **lưu thành công, không
  cảnh báo**, nhưng bản cấp đó **không bao giờ có hiệu lực**. Người dùng báo "không vào được", quản trị
  nhìn màn hình thấy đã cấp. ([PermissionEvaluator.cs:116-123](../../src/backend/PTKD.Application/Security/Authorization/Services/PermissionEvaluator.cs:116))
- Muốn người đó **có** quyền thì buộc phải cấp toàn cục — mà cấp toàn cục thì **thấy hết mọi công ty**.
  Chỉ có 2 nấc: mất sạch quyền, hoặc xem được tất cả. **Không có nấc "chỉ công ty mình".**
- Giao diện quản trị hiện **chỉ tạo được vai trò/nhóm quản trị phạm vi TOÀN CỤC** — form gửi
  `companyId` cứng bằng `null`, chọn COMPANY thì backend trả 400.
  ([RoleManagementPage.tsx:81](../../src/frontend/src/roleManagement/RoleManagementPage.tsx:81))

Nói cách khác: **trạng thái mặc định của hệ thống hiện nay đúng ngược ý đồ số 1 của anh**, và ý đồ số 2
chưa tồn tại như một cơ chế.

---

## 2. TRẢ LỜI THẲNG 4 Ý ĐỒ CỦA ANH

### Ý 1 — "Tài khoản công ty nào chỉ xem công ty đó"

| | |
|---|---|
| **Hiện trạng** | Không có chốt chặn nào ở tầng nền. `PermissionEvaluator` **không đọc bảng phân công công ty một lần nào** trong suốt 130 dòng đánh giá quyền. Header `X-Company-Id` do client tự khai, bộ lọc chỉ kiểm "có gửi không" và "có phải số không". |
| **Khoảng cách** | 14 lỗ xem/ghi chéo công ty đã xác nhận bằng code. Trong đó **6 lỗ ở mức GHI** — sửa được dữ liệu công ty khác, gồm cả `transfer-owner` và `owner-death` (chuyển quyền sở hữu mộ — hệ quả pháp lý thật). |
| **Cần đổi** | Ba việc, theo thứ tự: (a) có nguồn sự thật duy nhất về "công ty của tôi"; (b) bộ lọc phân quyền xác thực `X-Company-Id`; (c) truy vấn dữ liệu lọc theo tập công ty đó. |

### Ý 2 — "Set toàn cục thì mới xem được công ty khác"

| | |
|---|---|
| **Hiện trạng** | **Cơ chế này chưa tồn tại.** `ScopeType = GLOBAL` hiện chỉ quyết định *đậu/rớt* khi kiểm quyền, **không hề lọc dữ liệu**. ([PermissionEvaluator.cs:134,150,168,184](../../src/backend/PTKD.Application/Security/Authorization/Services/PermissionEvaluator.cs:134)) |
| **Khoảng cách** | Ngược lại hoàn toàn: hiện tại quyền toàn cục là **mặc định bắt buộc**, không phải ngoại lệ được chọn. Đội phát triển đã phải đẻ riêng một mã quyền `WORKFLOW_VIEW_ALL_COMPANIES` (V0036) chỉ để diễn đạt "xem xuyên công ty" — đúng thứ mà chữ GLOBAL lẽ ra phải diễn đạt. Cứ theo lối này, danh mục sẽ phình ra một cặp `*_ALL_COMPANIES` cho từng mã nghiệp vụ. |
| **Cần đổi** | Chuyển phạm vi từ **thuộc tính của mã quyền** thành **thuộc tính của lần cấp** (ô ma trận), rồi thêm một tầng lọc dữ liệu lấy phạm vi từ kết quả kiểm quyền. |

### Ý 3 — "Tách quyền THẤY MENU và quyền LÀM"

| | |
|---|---|
| **Hiện trạng** | Chưa có khái niệm này ở **cả hai tầng**. Menu đang dùng chính **mã hành động** để quyết định hiển thị: muốn ai đó *thấy* màn hình Thanh toán, buộc phải cấp cho họ quyền **TẠO phiếu thu**. Backend cũng vậy — `PaymentTransactionController.cs:90` dùng `PAYMENT_CREATE_DRAFT` để gác endpoint **liệt kê**. Kế toán trưởng "chỉ xem, không tạo" là **bất khả thi** với ma trận hiện tại. |
| **Tin tốt** | Danh mục **đã có sẵn** các cặp VIEW/MANAGE tách bạch — nhưng 11–15 mã trong số đó (tuỳ cách đếm) **chưa được nối vào code**, nằm chết. Ví dụ `ORGANIZATION_COMPANY_VIEW` nằm không dùng, trong khi `CompaniesController.cs:15` bắt cả endpoint GET phải có quyền **QUẢN LÝ**. |
| **Còn thiếu** | Mã VIEW cho Thanh toán và Đối soát (chưa có). Và các mã quá tải: `GRAVE_UPDATE` gánh **6 hành động** khác nhau, `TAG_MANAGE` gánh 5, `CUSTOMER_CARE_PACKAGE_MANAGE` gánh 3. Không có `action_code = DELETE` nào trong toàn bộ 67 quyền — mọi thao tác xoá đi ké quyền sửa. |

### Ý 4 — "Check lại tính logic của ma trận"

Có **4 mâu thuẫn logic ở mức nền**, mỗi cái đủ để làm ma trận nói dối người quản trị:

1. **Ba bản "quyền hiệu dụng" khác nhau cho cùng một câu hỏi.** `EvaluateInternalAsync` (chặn API),
   `GetEffectivePermissionsInternalAsync` (dựng menu), và một bản thứ ba trong
   `SecurityAdminService.cs:696-710` (màn hình quản trị "xem quyền của người khác"). Ba thuật toán,
   ba kết quả. Bản thứ ba **không lọc công ty của phòng ban và không lọc phòng ban đã vô hiệu hoá** —
   tức **màn hình dùng để kiểm tra ma trận lại báo cáo rộng hơn thực tế**.
2. **Menu và API lệch nhau cả hai chiều.** Có trường hợp menu hiện mà bấm vào 403; có trường hợp menu
   ẩn mà người dùng thực ra có quyền. (Ví dụ cụ thể bằng mã có thật ở mục 3.3.)
3. **`/me/permissions` trả sai thứ.** Nó trả `data_scope` của **danh mục** và gọi đó là "phạm vi quyền
   của tôi". Hậu quả đo được: **9 mã quyền bị frontend hỏi sai phạm vi → luôn trả false → cả nhóm menu
   "Thanh toán" và ~12 nút hành động chết vĩnh viễn.** ([AuthController.cs:338](../../src/backend/PTKD.Api/Controllers/AuthController.cs:338))
4. **Thứ tự ưu tiên DENY > Nhóm QT > Cá nhân > Vai trò > Phòng ban chỉ có ý nghĩa ở DENY.** Bốn nhánh
   còn lại là phép HOẶC thuần — đảo thứ tự không đổi kết quả. Và **DENY phạm vi COMPANY hoàn toàn câm
   với mọi quyền `data_scope = GLOBAL`**: cấm mà không cấm được.

---

## 3. VẤN ĐỀ GỐC — BA CHỮ "GLOBAL", BA NGHĨA KHÁC NHAU

Đây là chỗ mô hình trong đầu anh và mô hình trong code lệch pha.

| Nơi khai | Chữ GLOBAL nghĩa là gì **trong code hiện tại** | Ai sửa được |
|---|---|---|
| `Permissions.data_scope` — dòng danh mục | *"Mã quyền này **chỉ được đánh giá khi KHÔNG có công ty**"*. Đây là **ràng buộc kỹ thuật về cách gọi**, không phải phạm vi dữ liệu. | Chỉ sửa bằng migration SQL |
| `Roles / AdminGroups / UserIndividualPermissions.scope_type` — **ô trong ma trận** | *"Lần cấp này khớp với mọi công ty"*. **Đây mới là phạm vi, và đây là thứ anh đang nghĩ tới.** | Quản trị viên |
| `PermissionScope` enum — trên endpoint | *"Endpoint này không đọc header X-Company-Id"*. | Chỉ sửa bằng code |

**Ba cái này khoá lẫn nhau theo kiểu nghịch.** Chuỗi nhân quả:

```
data_scope = GLOBAL (dòng danh mục)
   └─> endpoint BUỘC phải khai PermissionScope.Global, nếu khai Company thì 403 100%
         └─> companyId truyền xuống LUÔN LUÔN là null
               └─> mọi bản cấp scope_type = COMPANY đều KHÔNG khớp
                     └─> ô ma trận của quản trị viên trở nên VÔ NGHĨA
```

**Bằng chứng sống là một vòng tránh trong chính mã nguồn:** `WorkflowRuntimeService.cs:223-224` gọi
`EvaluateAsync` **hai lần** — một lần với công ty của hồ sơ, một lần với `null`. Vế đầu **vĩnh viễn
false** vì `WORKFLOW_VIEW` khai `data_scope = GLOBAL`. Lập trình viên đã phải viết vòng tránh cho một
mô hình không cho phép diễn đạt điều mình muốn.

**Nói gọn cho dễ nhớ:** hệ thống đang dùng cột `data_scope` để trả lời hai câu hỏi khác hẳn nhau —
*"quyền này áp cho dữ liệu phạm vi nào"* và *"quyền này có được kiểm kèm công ty không"*. Trộn hai câu
hỏi vào một cột là gốc của toàn bộ 79 phát hiện.

---

## 4. MÔ HÌNH ĐỀ XUẤT

Tách bạch **ba trục** vốn đang bị trộn làm một.

### Trục A — Mã quyền = MODULE × HÀNH ĐỘNG (không mang phạm vi)

Mã quyền chỉ trả lời *"làm được gì"*. Quy ước đặt tên:

| Loại | Mẫu | Ví dụ |
|---|---|---|
| Xem (mở menu + đọc danh sách/chi tiết) | `<MODULE>_VIEW` | `PAYMENT_VIEW`, `GRAVE_VIEW` |
| Hành động cụ thể | `<MODULE>_<VERB>` | `PAYMENT_CREATE_DRAFT`, `GRAVE_TRANSFER_OWNER`, `GRAVE_ATTACHMENT_MANAGE` |
| Dữ liệu nhạy cảm | `<MODULE>_VIEW_SENSITIVE` | đã có sẵn cho khách hàng |

`data_scope` **thôi làm nhiệm vụ điều khiển**, chỉ còn là **nhãn phân loại** (quyền quản trị hệ thống
vs quyền dữ liệu nghiệp vụ) để giao diện gom nhóm.

### Trục B — Phạm vi = thuộc tính của Ô MA TRẬN (đây là ý 2 của anh)

Mỗi lần cấp quyền (vai trò / nhóm quản trị / cá nhân / chuẩn phòng ban) mang **một trong hai** phạm vi:

- **`COMPANY` — mặc định.** Chỉ áp cho công ty được chỉ định.
- **`GLOBAL` — ngoại lệ có chủ ý.** Áp cho mọi công ty. Nên đánh dấu **nhạy cảm**, bắt buộc nhập lý do,
  và chỉ người có quyền riêng mới cấp được.

Bảng đã có sẵn cột `scope_type` + `company_id` cho cả 3 loại cấp phát — **không cần thêm bảng mới cho
phần này**. Việc cần làm là làm cho nó **thật sự có tác dụng**.

### Trục C — Thi hành hai tầng

Đây là phần còn thiếu hoàn toàn hiện nay.

**Tầng 1 — Cổng (có quyền hay không):** như hiện tại, nhưng bỏ 2 chốt cứng `data_scope`.

**Tầng 2 — Lọc dữ liệu (mới):** đổi chữ ký của engine từ *"có/không"* sang *"có, và ở những công ty nào"*:

```csharp
// Thay cho: Task<bool> EvaluateAsync(userId, code, companyId)
public sealed record PermissionScopeResult(
    bool Granted,
    bool IsGlobal,             // true = xuyên công ty (ý đồ số 2 của anh)
    IReadOnlyList<long> CompanyIds);

Task<PermissionScopeResult> ResolveAsync(long userId, string permissionCode, CancellationToken ct);
```

Mọi truy vấn danh sách nhận `PermissionScopeResult` và tự gắn `WHERE company_id IN (...)`, trừ khi
`IsGlobal`. Mọi truy vấn ghi nạp bản ghi đích trước, lấy `CompanyId` **của chính bản ghi đó**, rồi mới
đối chiếu.

> **Về EF Core global query filter:** em **không khuyến nghị** dùng cho việc này. Global query filter
> gắn vào `DbContext` nên rất dễ bị `IgnoreQueryFilters()` vô hiệu hoá âm thầm, và nó không diễn đạt
> được "trừ khi có quyền global". Dùng một helper tường minh dễ rà soát hơn nhiều.

### Điểm chốt an ninh bắt buộc — xác thực `X-Company-Id`

Không có việc này thì **mọi bộ lọc phía dưới đều là hàng rào giấy**. Hiện `SecurityAdminService.cs:807-814`
**đã có sẵn** hàm kiểm tư cách thành viên công ty — nhưng chỉ dùng khi **cấp quyền**, không dùng khi
**phục vụ request**. Việc cần làm là nâng nó lên thành dịch vụ dùng chung và gọi ngay tại bộ lọc.

### Frontend

`/me/permissions` đổi từ `{ code, scope }` (sai) sang:

```json
{ "code": "CUSTOMER_VIEW_BASIC", "isGlobal": false, "companyIds": [5, 7] }
```

- **Menu** dựng theo mã `*_VIEW`.
- **Nút hành động** gác theo mã hành động tương ứng.
- **`ProtectedRoute`** gác theo mã VIEW của trang (hiện **không gác quyền gì** — 11 trang gõ URL tay là vào).
- `[RequirePermission]` hiện `AllowMultiple = false` nên không diễn đạt được *"cần XEM **và** một trong
  các HÀNH ĐỘNG"* → cần bổ sung `[RequireAnyPermission]`.

---

## 5. KẾ HOẠCH THI CÔNG — AN TOÀN NHẤT TRƯỚC

### GĐ 0 — CẦM MÁU (nhỏ, độc lập, làm được ngay, không đụng mô hình)

| Việc | File | Rủi ro |
|---|---|---|
| Vá 3 lỗ gói chăm sóc: `GET ?customerId/graveId` (đọc chéo), `POST /{id}/cancel` (ghi chéo), `POST /{id}/assign-grave` (ghi chéo) | `CustomerCarePackageService.cs:36-60, 237-312` | Thấp — có sẵn MẪU 3 ở `PaymentTransactionController` |
| Vá `PUT /api/v2/tags/customer/{id}` và `/grave/{id}` — đây là **đặt lại toàn bộ** thẻ, một lệnh gọi xoá sạch thẻ của công ty khác | `TagsController.cs:58-74` | Thấp |
| Vá lỗ **còn sót** sau 2 commit trước: `GET /workflows/instances/{id}/actions` chưa gọi `IsInstanceCompanyAccessibleAsync` | `WorkflowRuntimeService.cs:834-887` | Rất thấp — helper đã có sẵn |
| Gắn `[RequirePermission(WORKFLOW_REJECT)]` cho endpoint từ chối — hiện **không gác gì**, frontend là lớp chặn duy nhất | `WorkflowRuntimeController.cs:109-114` | Thấp |
| Bỏ ghim cứng `companyId = 1` ở 3 trang Thanh toán/Đối soát | `PaymentListPage.tsx:15` | Thấp |

**Kiểm chứng:** unit test backend + `npm run build` + đăng nhập thử 2 tài khoản khác công ty.

### GĐ 1 — NỀN TẢNG NGỮ CẢNH CÔNG TY (một chỗ chặn cho tất cả)

1. Nâng `WorkflowRuntimeService.GetMyCompanyIdsAsync` (đang là `private static`, chỉ 1 nơi dùng được)
   thành `ICompanyContextService` dùng chung. **Hiện có SÁU cách khác nhau để lấy "công ty của tôi"**,
   không cách nào là chuẩn — đây là nguồn của việc vá chỗ này sót chỗ kia.
2. `PermissionAuthorizationFilter` xác thực `X-Company-Id` thuộc tập công ty người gọi → 403 nếu không.
3. **Bổ sung màn hình gán công ty cho người dùng.** Backend đã có đủ endpoint
   (`api/v2/organizations/users/{userId}`) nhưng **frontend không có file nào gọi** — hiện muốn cho một
   nhân viên thuộc công ty nào phải **chạy SQL tay**. Không có dữ liệu này thì ý đồ 1 không có gì để dựa vào.

### GĐ 2 — SỬA MÔ HÌNH PHẠM VI (ý 1 + ý 2)

1. Bỏ 2 chốt cứng `data_scope` ở `PermissionEvaluator.cs:116-123`; đổi engine sang trả `PermissionScopeResult`.
2. **Hợp nhất 3 bản "quyền hiệu dụng" về MỘT hàm.** Đây là việc rẻ nhất có tác động lớn nhất tới ý 4.
3. Sửa `/me/permissions` trả `isGlobal` + `companyIds` thật.
4. Cho form Vai trò / Nhóm quản trị gửi được `companyId` (hiện ghim `null`, và đã tạo GLOBAL rồi thì
   **vĩnh viễn không sửa được** thành COMPANY qua giao diện).
5. Đổi `data_scope` của **18 mã nghiệp vụ** từ GLOBAL sang COMPANY, kèm **script cấp lại quyền**.
6. Áp bộ lọc công ty vào truy vấn `customers` và `customer-care-packages` theo MẪU 1 (`SearchInstancesAsync`).

### GĐ 3 — TÁCH VIEW/ACTION (ý 3)

1. Thêm mã VIEW còn thiếu (`PAYMENT_VIEW`, `RECONCILIATION_VIEW`, `TAG_VIEW`).
2. Nối 11–15 mã đang chết vào code — **rẻ nhất và đúng ý 3 nhất**: `ORGANIZATION_COMPANY_VIEW`,
   `ORGANIZATION_DEPARTMENT_VIEW`, các cặp VIEW/MANAGE của nhóm bảo mật.
3. Tách các mã quá tải: `GRAVE_UPDATE` (6 hành động) → `GRAVE_OCCUPANT_MANAGE`,
   `GRAVE_EMERGENCY_CONTACT_MANAGE`, `GRAVE_TRANSFER_OWNER`, `GRAVE_ATTACHMENT_MANAGE`.
4. Frontend: menu theo `*_VIEW`, nút theo mã hành động, `ProtectedRoute` gác quyền.

### GĐ 4 — MÔ-ĐUN MỘ: CẦN QUYẾT ĐỊNH NGHIỆP VỤ TRƯỚC

**Bảng `dbo.Graves` không có cột công ty nào** — `GraveService.cs` không có một tham chiếu `CompanyId`
nào trong cả 805 dòng. Nghĩa là hiện tại **không tồn tại khái niệm "mộ này thuộc công ty nào"**.
16/16 action của `GravesController` đều chạy phạm vi toàn cục.

Không thể siết mô-đun Mộ theo công ty cho tới khi anh chốt: **mộ thuộc công ty qua đường nào** — qua
nghĩa trang? qua khu (A–L)? hay gán trực tiếp? Đây là câu hỏi nghiệp vụ, AI không tự đặt.

---

## 6. RỦI RO & ĐÁNH ĐỔI — NÓI THẬT

| Rủi ro | Mức | Ghi chú |
|---|---|---|
| **Đổi `data_scope` làm đứt mọi grant hiện có** | CAO | Mọi bản cấp GLOBAL đang chạy sẽ hết tác dụng với 18 mã đó. Bắt buộc phải có script cấp lại + đối chiếu quyền trước/sau. Không làm giữa giờ làm việc. |
| **Test đỏ hàng loạt** | TRUNG BÌNH | 265 test backend + 546 test frontend đang xanh. Một số test hiện **đang khẳng định hành vi cũ là đúng** — sẽ đỏ và đó là tín hiệu đúng, không phải hỏng. |
| **Người dùng mất quyền đột ngột giữa pilot** | CAO | Nên làm trên nhánh riêng, có bản chiếu "ai có quyền gì trước / sau" để đối chiếu trước khi áp. |
| **Vá lẻ tạo cảm giác an toàn giả** | TRUNG BÌNH | GĐ 0 vá 5 lỗ nhưng **không** giải quyết gốc. Phải nói rõ với người dùng: sau GĐ 0 hệ vẫn chưa "kín". |
| **Mô-đun Mộ không vá được ở GĐ 0–3** | — | Thiếu chiều dữ liệu. Phải chấp nhận mộ vẫn xem chéo cho tới khi có quyết định ở GĐ 4. |

**Điều em KHÔNG khuyến nghị:** làm cả 4 giai đoạn trong một đợt. Mô hình phân quyền là thứ hỏng thì
hỏng im lặng — mỗi giai đoạn nên có một vòng kiểm chứng riêng và một điểm dừng an toàn.

---

## 7. CÂU HỎI CẦN ANH QUYẾT

Chỉ liệt kê những câu mà **trả lời khác nhau sẽ dẫn tới thi công khác nhau**.

**Q1. Phạm vi nên là ô trong ma trận, hay tiếp tục đẻ mã quyền riêng?**
Hiện đang theo lối thứ hai (`WORKFLOW_VIEW_ALL_COMPANIES`).
→ *Khuyến nghị: **ô trong ma trận**.* Đúng ý anh, và tránh danh mục phình ra một cặp `*_ALL_COMPANIES`
cho từng mã. Đánh đổi: refactor engine lớn hơn.

**Q2. Có chấp nhận cấp lại quyền cho toàn bộ tài khoản sau khi đổi mô hình không?**
→ *Khuyến nghị: **có**, kèm bản chiếu trước/sau và một cửa sổ bảo trì.* Không có đường nào tránh được
việc này nếu chọn Q1 = ô ma trận.

**Q3. Mộ thuộc công ty qua đường nào?** (nghĩa trang / khu / gán trực tiếp / mộ là tài sản dùng chung
không thuộc công ty nào)
→ *Không khuyến nghị — đây là quyết định nghiệp vụ thuần.* Nhưng cần chốt trước GĐ 4.

**Q4. Một người có được thuộc nhiều công ty không?**
Dữ liệu hiện **cho phép**, và điều đó đang gây lỗi: người đa công ty mở app lên thấy **menu thiếu**
cho tới khi tự bấm chọn công ty.
→ *Khuyến nghị: **có, giữ nguyên**, nhưng phải chọn công ty mặc định lúc đăng nhập thay vì để rỗng.*

**Q5. Làm GĐ 0 ngay bây giờ, hay chờ chốt xong mô hình rồi làm một mạch?**
→ *Khuyến nghị: **làm GĐ 0 ngay**.* 5 lỗ đó đang chảy máu thật, cách vá đã có khuôn mẫu trong repo,
và **không** phụ thuộc vào quyết định Q1–Q4 nên không sợ công cốc.

---

## Phụ lục — thống kê phát hiện

| Mức | Số lượng |
|---|---|
| HIGH | 32 |
| MEDIUM | 33 |
| LOW | 11 |
| INFO | 3 |
| **Tổng sống sót** | **79** |
| Bị bác sau kiểm chứng | 1 |

| Loại | Số lượng |
|---|---|
| Mâu thuẫn logic | 19 |
| Xem/ghi chéo công ty | 14 |
| Thiếu tách VIEW/ACTION | 13 |
| Lệch so với ý đồ chủ sở hữu | 12 |
| Khuyết tật mô hình phạm vi | 11 |
| Trùng tên gây nhầm | 5 |
| Khác | 5 |

**Chưa kiểm chứng, cần rà riêng:** vòng phê bình độ đầy đủ (kiểm Worker/Bootstrap, stored procedure,
view, trigger, endpoint report/export, thông báo/chuông, dữ liệu seed) **không chạy được vì hết hạn mức
phiên**. Không được đọc thành "đã sạch".
