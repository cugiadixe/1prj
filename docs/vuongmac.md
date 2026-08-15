# Vướng mắc & công việc — bàn giao phiên

> Cập nhật: **2026-08-15 (cuối phiên)**. Người soạn: AI (Claude).
> Đây là **bản nháp thảo luận**, không phải hồ sơ trình duyệt. Mọi mục cần người có thẩm quyền
> đánh giá trước khi kết tinh thành quyết định.
>
> Bản trước (phiên 2026-08-14, nội dung rà soát an ninh và câu hỏi thiết kế A/B/C) đã được
> **giải quyết trong phiên này** — xem mục 2. Nội dung cũ còn nguyên trong lịch sử Git tại
> commit `e1d1580`.

---

## 0. ĐANG Ở ĐÂU

**Nhánh:** `feature/permission-scope-redesign` (tách từ `feature/workflow-admin-usability`).
**Chưa push. Chưa gộp `main`.** Cây làm việc sạch.

**33 commit chưa gộp `main`** (`feature/phase-1-organization`), trong đó **5 commit của phiên này**:

| Commit | Nội dung |
|---|---|
| `b05215b` | Menu thanh bên: mở đúng nhóm chứa trang đang xem, accordion *(việc tồn từ phiên trước, không liên quan phân quyền)* |
| `c5d9599` | **Phân quyền: phạm vi là Ô TRONG MA TRẬN** — tầng nền |
| `403e691` | **V0037** — chuyển dữ liệu phân quyền sang mô hình mới |
| `33c2783` | **Frontend** — bỏ tham số `scope` phỏng đoán |
| `33aaffe` | **Vá 5 lỗ xem/ghi chéo công ty** + gác quyền endpoint từ chối |

**CSDL:** `PTKD_DEV` đã áp tới **V0037**. Dữ liệu phân quyền **đã ở mô hình mới**.

**Kiểm chứng gần nhất:**

| Hạng mục | Kết quả |
|---|---|
| Build backend | 0 lỗi |
| Unit test backend | **268/268** |
| `npm run build` frontend | xanh |
| Test frontend | **551/551** |
| Đăng nhập thử trên giao diện | ❌ **CHƯA** — cần anh làm |

**Việc đầu tiên khi quay lại:** anh đăng nhập thử, kiểm menu và vài nút hành động còn hiện đúng
không. AI không tự đăng nhập (mục 6.3).

---

## 1. QUYẾT ĐỊNH CỦA ANH BÁCH TRONG PHIÊN NÀY

Anh nêu 4 ý đồ về phân quyền, và chốt 4 quyết định thiết kế:

| # | Quyết định | Trạng thái |
|---|---|---|
| 1 | **Phạm vi là Ô TRONG MA TRẬN** — mỗi lần cấp chọn COMPANY (mặc định) hay GLOBAL, thay vì đẻ thêm mã quyền riêng kiểu `*_ALL_COMPANIES` | ✅ đã thi công |
| 2 | **Mộ thuộc công ty QUA NGHĨA TRANG** | ⛔ chặn — xem mục 3.1 |
| 3 | **Tự động chuyển** grant GLOBAL hiện có sang COMPANY | ✅ đã chạy, đã kiểm chứng |
| 4 | **Không vá lẻ** — chốt mô hình rồi làm một mạch | ✅ đã theo |

Bốn ý đồ gốc của anh, để đối chiếu về sau:

1. Tài khoản công ty nào thì mặc định chỉ xem công ty đó.
2. Set quyền toàn cục trong ma trận thì mới xem được công ty khác.
3. Tách quyền **nhìn thấy menu** (view) và quyền **thực hiện** (action); một màn hình có thể có
   nhiều action.
4. Rà lại tính logic của ma trận phân quyền.

---

## 2. ĐÃ LÀM XONG

### 2.1. Rà soát toàn diện (trả lời ý 4)

Chạy 5 mặt cắt song song, mỗi mặt cắt có một vòng kiểm chứng đối kháng riêng:
**79 phát hiện sống sót** (32 mức cao), 1 bị bác. Báo cáo đầy đủ:
[`docs/reviews/ra-soat-ma-tran-phan-quyen-2026-08-15.md`](reviews/ra-soat-ma-tran-phan-quyen-2026-08-15.md).

**Vấn đề gốc tìm ra:** hệ có **ba chữ "GLOBAL" mang ba nghĩa khác nhau**, và cái quyết định lại
là cái quản trị viên KHÔNG sửa được:

```
data_scope (dòng danh mục) = "chỉ được đánh giá khi KHÔNG có công ty"
   └─> endpoint buộc phải khai PermissionScope.Global
         └─> companyId truyền xuống LUÔN là null
               └─> mọi lần cấp scope_type = COMPANY đều không khớp
                     └─> ô ma trận của quản trị viên trở nên VÔ NGHĨA
```

Hệ quả: cấp quyền cho ai đó ở phạm vi "chỉ công ty A" thì hệ **lưu thành công, không cảnh báo**,
nhưng bản cấp **không bao giờ có hiệu lực**.

### 2.2. Tầng nền (`c5d9599`)

- Bỏ 2 chốt cứng `data_scope`. **Đã đối chiếu toàn bộ 106 attribute** với danh mục trước khi bỏ:
  không cặp nào lệch → thao tác trơ, không nới quyền ngoài ý muốn.
- `ResolveAsync` trả `PermissionScopeResult {Granted, IsGlobal, CompanyIds, DeniedCompanyIds}` —
  nơi gọi nay biết phải lọc dữ liệu theo công ty nào.
- **Gộp 3 bản "quyền hiệu dụng" về 1.** Bản thứ ba (phục vụ *màn hình kiểm tra ma trận*) trước
  đây báo cáo **rộng hơn thực tế** — tức công cụ dùng để rà soát đang dẫn người rà đi sai.
- Quyền chuẩn phòng ban nay mang phạm vi công ty **của phòng đó**.
- Lệnh CẤM theo công ty nay **cắn được cả người cấp toàn cục**.
- `PermissionAuthorizationFilter` **không còn tin `X-Company-Id`** do client tự khai.
- `ICompanyContextService` — nguồn sự thật duy nhất "công ty của tôi" (trước có **sáu** cách).

### 2.3. Migration V0037 (`403e691`) — đã chạy và kiểm chứng

| Kiểm tra sau khi chạy | Kết quả |
|---|---|
| Người bị mất quyền | **0** |
| Quyền admin phủ đủ 5/5 công ty | đủ |
| Admin còn quyền quản trị toàn cục | **17 mã** — không có nguy cơ khoá chết |
| Bản cấp hiệu lực | 74 → 158 (đóng 24 GLOBAL, tạo 96 COMPANY + 12 mã tách) |

Danh sách 18 mã nghiệp vụ được chuyển là **khoá cứng trong bảng tạm**, kèm chốt `THROW` nếu lỡ
lọt mã `SECURITY_*`/`ORGANIZATION_*`. Theo lối copy-based: bản cũ **không bị xoá**, chỉ đóng lại.

### 2.4. Frontend (`33c2783`)

Bỏ tham số `scope` phỏng đoán ở **90 lời gọi / 30 file**. Đây là thứ khiến 9 mã bị hỏi sai phạm
vi nên phép kiểm luôn false — cả cụm menu **Thanh toán** và ~12 nút hành động chết vĩnh viễn mà
không có thông báo lỗi nào.

### 2.5. Vá 6 lỗ an ninh đã xác nhận (`33aaffe`)

| Lỗ | Loại |
|---|---|
| `GET /customer-care-packages?customerId=&graveId=` | đọc chéo công ty |
| `POST /customer-care-packages/{id}/cancel` | **GHI** chéo công ty |
| `POST /customer-care-packages/{id}/assign-grave` | **GHI** chéo công ty |
| `PUT /tags/customer/{id}` | **GHI** chéo (đặt lại toàn bộ → xoá sạch thẻ công ty khác) |
| `GET /workflows/instances/{id}/actions` | lỗ **còn sót** sau 2 commit vá trước |
| `POST /workflows/.../reject` | **không gác quyền gì cả** — giao diện là lớp chặn duy nhất |

---

## 3. VƯỚNG MẮC — CẦN ANH QUYẾT

### 3.1. ⛔ CHẶN: nghĩa trang thuộc công ty nào?

Anh đã chốt "mộ thuộc công ty **qua nghĩa trang**". Nhưng **hệ chưa có bảng nghĩa trang** —
`dbo.Graves` chỉ có cột `zone` (khu A–L) dạng chuỗi, không có thực thể nào để gắn công ty.

Việc phải làm: tạo bảng `Cemeteries` (có `company_id`), thêm `cemetery_id` vào `Graves`, gán toàn
bộ mộ hiện có vào **một** nghĩa trang (12 khu A–L đều thuộc cùng một nghĩa trang).

**Cần anh cho biết nghĩa trang đó thuộc công ty nào** (hệ có 5 công ty: id 31–35). Gợi ý để anh
đối chiếu: công ty **35** là nơi có phòng CSKH đang được cấp quyền khách hàng + gói chăm sóc qua
chuẩn phòng ban — dấu hiệu đây là đơn vị vận hành nghiệp vụ nghĩa trang. Nhưng đây là **suy đoán
từ dữ liệu phân quyền**, không phải căn cứ nghiệp vụ, nên AI không tự đặt.

**Chưa trả lời thì mô-đun Mộ vẫn xem/ghi chéo công ty.**

### 3.2. Người dùng thuộc nhiều công ty — có tự chọn sẵn công ty không?

Hiện theo **luật 1B.1-M**: một công ty thì tự chọn, nhiều công ty thì bắt chọn thủ công.

Trong phiên này AI đã **thử đổi** thành tự chọn công ty mặc định, và **một test bắt được** — luật
này đã ghi trong tài liệu nên AI **đã trả lại nguyên trạng**, không tự lật.

**Cần anh quyết:** giữ nguyên (an toàn hơn, chọn nhầm công ty nay là lỗi an ninh chứ không chỉ
bất tiện) hay đổi thành tự chọn công ty mặc định (tiện hơn).
*Khuyến nghị: giữ nguyên.* Triệu chứng "menu biến mất khi chưa chọn công ty" đã xử lý bằng cách
khác rồi.

### 3.3. Sau khi khai báo cấu hình xong, có bỏ luật giảm giá trong code không? *(còn từ phiên trước)*

Luật *"có giảm giá thì phải duyệt"* đang chạy **hai tầng**: cấu hình quyết định nếu đã khai báo,
không thì lùi về luật cứng trong code (`DiscountAmount > 0`) làm lưới an toàn.

**Cần anh quyết:** sau khi khai báo xong quy trình bán gói, có bỏ hẳn lưới an toàn trong code
không? Bỏ thì cấu hình là nguồn sự thật duy nhất (sạch hơn); giữ thì an toàn hơn nhưng có hai chỗ
định nghĩa luật.

### 3.4. Thứ tự gộp `main` *(còn từ phiên trước, nay 33 commit)*

33 commit chưa gộp. Cần anh cho phép và cho biết có muốn tạo Pull Request để lưu vết review không.

### 3.5. Mỗi quy trình duyệt: ai duyệt, cấp nào? *(còn từ phiên trước)*

Chặn việc bật 4 nhóm quy trình còn lại (bán gói · in lại thẻ · đề xuất/sửa KH · gộp KH trùng).
Đây là quyết định nghiệp vụ, AI không tự đặt.

---

## 4. CÔNG VIỆC SẮP TỚI — theo thứ tự đề xuất

### Nhóm A — hoàn tất ý đồ 1 (mặc định chỉ xem công ty mình)

| # | Việc | Ghi chú |
|---|---|---|
| A1 | **`CustomersController` lọc theo công ty** | 10 action đều toàn cục, không mệnh đề lọc nào. `lookups/companies` trả **mọi** công ty — chính là bản đồ chỉ đường cho việc quét chéo |
| A2 | Bỏ ghim cứng `companyId = 1` ở 3 trang Thanh toán/Đối soát | `PaymentListPage.tsx:15`. Thêm nữa: `PaymentCreatePage` để người dùng **tự gõ** mã công ty |
| A3 | Bộ lọc công ty ở trang Khách hàng/Dịch vụ mặc định **RỖNG = xem tất cả** | Đúng ngược ý đồ 1 |
| A4 | **Mộ** — dựng `Cemeteries`, thêm `cemetery_id`, rồi lọc 16 action của `GravesController` | ⛔ chặn bởi 3.1 |

### Nhóm B — hoàn tất ý đồ 2 (chỉ global mới xem chéo)

| # | Việc | Ghi chú |
|---|---|---|
| B1 | **Cho form Vai trò / Nhóm quản trị tạo được phạm vi COMPANY** | Hiện form gửi `companyId` cứng bằng `null` → **chỉ đẻ ra được GLOBAL**; đã lỡ tạo GLOBAL thì vĩnh viễn không sửa được thành COMPANY qua giao diện. **Không có việc này thì anh chưa dùng được mô hình mới bằng giao diện** |
| B2 | Giao diện giải thích rõ "GLOBAL = xuyên công ty" + chốt ai được phép cấp GLOBAL | `is_sensitive`/`requires_reason` hiện chỉ là metadata trang trí |
| B3 | **Màn hình gán công ty cho người dùng** | Backend đã có đủ endpoint `api/v2/organizations/users/{userId}` nhưng **frontend không file nào gọi** — hiện muốn cho nhân viên thuộc công ty nào phải **chạy SQL tay** |

### Nhóm C — hoàn tất ý đồ 3 (tách view/action)

| # | Việc | Ghi chú |
|---|---|---|
| C1 | **Nối 4 mã VIEW/ACTION mới vào code** | ⚠️ Xem mục 5.1 — V0037 đã tạo mã nhưng **chưa nối**, nên hiện chúng là mã chết |
| C2 | Nối 11–15 mã đang nằm chết sẵn trong danh mục | Rẻ nhất, đúng ý 3 nhất. Ví dụ `CompaniesController`/`DepartmentsController` bắt cả endpoint **GET** phải có quyền **QUẢN LÝ**, đúng lúc `ORGANIZATION_*_VIEW` nằm không dùng |
| C3 | Menu dựng theo mã `*_VIEW` thay vì mã hành động | |
| C4 | **`ProtectedRoute` gác quyền** | Hiện **không gác quyền gì** — 11 trang gõ URL tay là vào |
| C5 | Thêm `[RequireAnyPermission]` | `AllowMultiple = false` nên không diễn đạt được *"cần XEM **và** một trong các HÀNH ĐỘNG"* |

### Nhóm D — dọn các phát hiện còn lại

| # | Việc |
|---|---|
| D1 | Bump `policy_version` khi **đổi công ty/phòng ban của người dùng** — đường ghi này đang quên, quyền cũ còn sống tới 5 phút |
| D2 | `CUSTOMER_CHANGE_REQUEST_ADMIN_VIEW`: mã **không tồn tại trong danh mục**, lại kiểm bằng loại claim **chưa bao giờ được phát** → quản trị viên không bao giờ xem được hồ sơ đề xuất, và không sửa được bằng cấu hình |
| D3 | Nút "Phân công lại" gọi endpoint đòi `X-Company-Id` nhưng client **không gửi** → luôn lỗi 400 |
| D4 | Bộ đếm chuông "chờ duyệt" trên Trang chủ gọi **sai đường dẫn API** nên luôn bằng 0 |
| D5 | Đính kèm mộ: tải nội dung và xoá **bỏ qua hoàn toàn `graveId`** trên đường dẫn |
| D6 | `CustomerMergeController` **thiếu `[Authorize]`** ở mức lớp |
| D7 | `GET /approval-authorities` trả **toàn bộ công ty** khi bỏ trống tham số lọc |
| D8 | Màn chẩn đoán quyền: hiển thị **sai phạm vi** (đọc trường backend không trả về); bị khoá vào công ty của chính quản trị viên |

### Nhóm E — việc lớn đã chốt từ phiên trước, chưa động tới

**Bật 4 nhóm quy trình còn lại** (quyết định 2026-08-14, phương án A): bán gói chăm sóc · in lại
thẻ · đề xuất/sửa thông tin KH · gộp KH trùng. Chặn bởi 3.5.

Riêng **gộp KH trùng** phải viết thêm code: `CustomerMergeService` **không hề gọi engine quy
trình**, yêu cầu gộp tạo xong nằm ở `DRAFT` vĩnh viễn.

---

## 5. ⚠️ NHỮNG GÌ **CHƯA** KÍN — đừng đọc thành "đã an toàn"

### 5.1. Bốn mã quyền mới đang là MÃ CHẾT

V0037 đã tạo `PAYMENT_VIEW`, `RECONCILIATION_VIEW`, `GRAVE_OCCUPANT_MANAGE`,
`GRAVE_EMERGENCY_CONTACT_MANAGE` **và đã cấp cho người đang có quyền gộp** — nên **không ai mất
quyền**. Nhưng **controller vẫn dùng mã cũ**, tức 4 mã này hiện **chưa được thi hành**.

Đây đúng là thứ mà chính báo cáo rà soát phê phán ("thêm mã để đó chỉ làm ma trận nói dối thêm").
Ghi lại thành việc C1 thay vì để lặng lẽ.

### 5.2. Hai mô-đun vẫn xem/ghi chéo công ty

- **Khách hàng** — `CustomersController` chưa lọc (việc A1).
- **Mộ** — 16/16 action toàn cục, gồm cả `transfer-owner` và `owner-death` là **chuyển quyền sở
  hữu tài sản, hệ quả pháp lý thật** (việc A4, chặn bởi 3.1).

### 5.3. Khoảng 20/79 phát hiện đã xử lý

Phần lớn mức HIGH liên quan mô hình phạm vi đã xong. **59 phát hiện còn lại chưa đụng tới** —
nhóm D ở trên chỉ liệt kê những cái đáng làm sớm, không phải toàn bộ.

### 5.4. Vòng phê bình độ đầy đủ CHƯA CHẠY

Agent kiểm "còn thiếu vùng nào" **chết vì hết hạn mức phiên**. Các vùng **chưa ai soi**:

- `PTKD.Worker` / `PTKD.Bootstrap` — có bỏ qua kiểm quyền không?
- Stored procedure / view / trigger trong `database/procedures`, `views`, `triggers` — có bypass
  phân quyền không?
- Endpoint report/export/lookup (dễ lộ dữ liệu hàng loạt).
- Engine duyệt: người duyệt công ty A có duyệt được hồ sơ công ty B không (`ApproverResolver`)?
- Thông báo/chuông và `me/activity` — có lọc theo công ty không?
- Dữ liệu seed — có tài khoản/vai trò nào được cấp GLOBAL rộng tay không?

**Kết quả rỗng KHÔNG có nghĩa là sạch.**

### 5.5. Chưa test trên giao diện thật

Toàn bộ kiểm chứng của phiên này là build + unit test + integration test. **Chưa ai đăng nhập
bấm thử.** Đặc biệt đáng nghi: menu và các nút hành động sau khi đổi hợp đồng `/me/permissions`.

---

## 6. VIỆC AI TỪ CHỐI / CỐ Ý KHÔNG LÀM

### 6.1. Không tự chọn sẵn công ty cho người dùng đa công ty
Đã thử, test bắt được, và đó là **luật 1B.1-M đã ghi trong tài liệu**. Đã trả lại nguyên trạng và
đưa lên thành câu hỏi 3.2 thay vì tự quyết.

### 6.2. Không đoán nghĩa trang thuộc công ty nào
Có thể suy từ dữ liệu phân quyền (công ty 35), nhưng đó là **suy đoán**, không phải căn cứ nghiệp
vụ. Đoán sai thì toàn bộ dữ liệu mộ gắn nhầm công ty.

### 6.3. Không tự đăng nhập, không tự tạo PR
Theo ràng buộc đã thống nhất. Mọi bước cần tài khoản đều do anh thực hiện.

### 6.4. Không chuyển `useCompany()` xuống sau nhánh kiểm quyền *(ghi lại từ phiên trước)*
Đặt hook sau một `return` sớm là **gọi hook có điều kiện** — React sẽ vỡ. Sửa theo hướng đó tạo ra
lỗi thật để vá một vấn đề chỉ tồn tại trong test.

---

## 7. GHI CHÚ KỸ THUẬT DỄ QUÊN

1. **`npm run build` biên dịch CẢ file test** (`tsc -b && vite build`). Bắt buộc chạy trước khi
   gộp `main`; không được chỉ tin `tsc --noEmit`.

2. **Build backend hỏng khi API đang chạy** — file `.dll` bị khoá. Phải dừng backend rồi mới build.

3. **Restart backend là mất phiên đăng nhập** — lần nào cũng phải đăng nhập lại.

4. **Backend trả UTC nhưng thiếu hậu tố `Z`** → JS hiểu nhầm thành giờ local, lệch 7 tiếng.
   Dùng `src/utils/datetime.ts`.

5. **Test "đóng băng phạm vi"** kiểu `expect('deleteApproverRule' in mod).toBe(false)` ghi lại
   *"giai đoạn này chưa xây"*, **không phải cấm thiết kế**. Phải đối chiếu tài liệu trước khi phá.

6. **Migrator chạy mọi batch `GO` trên cùng một kết nối và cùng một transaction** — nên bảng tạm
   `#temp` sống xuyên `GO`. Đã dựa vào tính chất này ở V0037.

7. **Chạy test frontend nên dùng `--no-file-parallelism`** nếu máy đang tải nặng.

8. **Kết nối CSDL dev bằng sqlcmd cần cờ `-C`** (tin chứng chỉ), nếu không sẽ lỗi SSL:
   `sqlcmd -S ".\SQLEXPRESS" -d PTKD_DEV -E -C -Q "..."`.
