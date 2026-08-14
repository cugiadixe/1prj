# Vướng mắc & câu hỏi cần anh Bách quyết

> Cập nhật: 2026-08-14 (cuối phiên). Người soạn: AI (Claude) — đây là **bản nháp thảo luận**,
> không phải hồ sơ trình duyệt. Mọi mục dưới đây cần người có thẩm quyền đánh giá trước khi
> kết tinh thành quyết định.

---

## 0. Đang ở đâu

**Nhánh:** `feature/workflow-admin-usability` (đã push), stack trên `main`.
**6 commit chưa gộp `main`:**

| Commit | Nội dung |
|---|---|
| `6e18017` | Nhóm 1 — làm cho việc SỬA quy trình khả thi trên giao diện |
| `dd48d12` | Nhóm 2 — bộ đánh giá điều kiện (khai báo luật không cần code) |
| `effedc4` | Nhóm 3 — lớp nền cho handler chỉ đổi trạng thái |
| `148f287` | Seed 2 quy trình còn thiếu + chuyển luật giảm giá ra cấu hình |
| `1f5d6ef` | Dọn 99 test frontend trôi do Việt hoá |
| `2adb2c8` | Định danh mục menu + nới timeout test |

**Migration đã áp vào CSDL dev:** tới `V0035`.
**Tình trạng kiểm chứng:** backend build 0 lỗi · unit test backend 265/265 PASS ·
`npm run build` XANH · toàn bộ test frontend **75/75 file, 546/546 PASS**.

**Việc đầu tiên nên làm khi quay lại:** đăng nhập thử Nhóm 1+2+3 trên giao diện, rồi
quyết định gộp `main`.

---

## 0b. ✅ QUYẾT ĐỊNH CỦA ANH BÁCH (cuối phiên 2026-08-14)

1. **Gộp `main`:** anh test giao diện trước (~15 phút), rồi mới gộp. Chưa gộp.
2. **Việc lớn tiếp theo: phương án A — BẬT CÁC QUY TRÌNH CÒN LẠI.** Biến năng lực đã xây
   thành tính năng chạy thật, thay vì xây thêm năng lực mới.
3. **Bật cả 4 nhóm quy trình:** bán gói chăm sóc (có giảm giá) · in lại thẻ · đề xuất khách
   hàng mới + sửa thông tin KH · gộp khách hàng trùng.

> Thứ tự thực hiện đề xuất (từ dễ tới khó): **bán gói** (chỉ cần khai báo điều kiện
> `Số tiền giảm giá > 0`) → **in lại thẻ** (chỉ khai báo) → **đề xuất/sửa KH** (chỉ khai báo)
> → **gộp KH trùng** (phải VIẾT THÊM CODE nối luồng vào engine).
>
> **Chặn trước khi làm:** mỗi quy trình cần anh cho biết **ai duyệt, cấp nào** — đây là quyết
> định nghiệp vụ, AI không tự đặt.

### ⛔ Việc dở dang: rà soát an ninh chưa chạy được

Cuối phiên đã cho chạy rà soát *"backend có thật sự chặn xem chéo công ty không"* (sau khi gỡ
guard frontend). **Cả 4 luồng đều thất bại do hết hạn mức phiên làm việc — KHÔNG có kết quả.**
Kết quả rỗng KHÔNG có nghĩa là sạch. Cần chạy lại. Bốn mặt cắt định soi:
`ServiceController`/`ServiceTypeController` · `CustomerCarePackagesController`/
`CarePackageRequestsController` · cơ chế `PermissionAuthorizationFilter` + `PermissionEvaluator`
· endpoint mới `GET /workflows/instances` (gắn quyền GLOBAL, **không lọc theo công ty**, mà
`businessEntityLabel` có chứa tên khách hàng + mã KH — đây là chỗ em nghi nhất).

---

## 1. ❓ CÂU HỎI CẦN ANH TRẢ LỜI

### 1.1. Guard chống xem chéo công ty — đã gỡ, xin xác nhận lại cho chắc

Ngày 2026-08-14 anh trả lời **"Gỡ bỏ Guard nhé"** và em đã giữ nguyên trạng thái đã gỡ.

Ghi lại cho rõ để sau này không ai hiểu nhầm: guard này bị gỡ khỏi mã nguồn từ commit
`4cb4d9f` (**trước** phiên làm việc này, không phải do đợt sửa vừa rồi). Nay
`ServiceDetailPage` và `ServiceListPage` **cho phép xem dịch vụ của công ty khác**, dựa vào
việc backend đã kiểm quyền `SERVICE_VIEW` theo công ty của dịch vụ.

> **Rủi ro còn lại:** toàn bộ việc chặn xem chéo công ty giờ **chỉ nằm ở backend**. Nếu có
> endpoint nào quên kiểm, frontend sẽ không còn lớp chắn thứ hai. Đề nghị: khi rảnh nên rà
> lại các endpoint dịch vụ xem có kiểm quyền theo công ty đầy đủ chưa.

Cảnh báo ở trang **tạo mới** dịch vụ vẫn giữ — đó là ca khác (tạo mới bắt buộc phải biết
tạo cho công ty nào), không phải guard xem chéo.

### 1.2. Sau khi khai báo cấu hình xong, có bỏ luật giảm giá trong code không?

Hiện luật *"có giảm giá thì phải duyệt"* hoạt động **hai tầng**:

1. Nếu quy trình `SELL_CARE_PACKAGE` đã có liên kết cấu hình → **cấu hình quyết định**.
2. Nếu **chưa** cấu hình → lùi về luật cũ trong code (`DiscountAmount > 0`) làm **lưới an toàn**.

Em cố ý làm vậy để không có hồ sơ nào âm thầm thoát phê duyệt trong lúc chưa khai báo.

**Cần anh quyết:** sau khi anh khai báo xong quy trình bán gói (điều kiện
`Số tiền giảm giá > 0`), có muốn **bỏ hẳn** lưới an toàn trong code không? Bỏ thì cấu hình
là nguồn sự thật duy nhất (sạch hơn); giữ thì an toàn hơn nhưng có hai chỗ định nghĩa luật.

### 1.3. Thứ tự gộp về `main`

6 commit trên một nhánh. Có thể gộp thẳng (fast-forward) vì `main` chưa đi trước.
Cần anh cho phép, và cho biết có muốn tạo Pull Request để lưu vết review không.

---

## 2. ⚠️ NỢ KỸ THUẬT — CHƯA LÀM, CÓ LÝ DO

### 2.1. Gộp khách hàng trùng: khai báo được rồi nhưng **chưa có nút gửi duyệt**

`CUSTOMER_MERGE_DUPLICATE` nay đã có trong danh mục quy trình (V0035), nên **cấu hình
được**. Nhưng `CustomerMergeService` **không hề gọi engine quy trình** — không có endpoint
submit/approve/reject. Yêu cầu gộp tạo xong nằm ở `DRAFT` vĩnh viễn.

Việc cần làm: nối luồng gộp vào engine (giống cách `CardReprintRequestService` đã làm).
Ước lượng: vừa, chủ yếu là thêm endpoint + gọi engine, handler đã có sẵn.

### 2.2. Bốn quy trình có handler nhưng **chưa ai khai báo cấu hình**

Danh mục có 8 quy trình. Chỉ `ASSIGN_CARE_PACKAGE` là có định nghĩa + liên kết thật
(seed từ V0031). Bốn quy trình sau **code sẵn sàng nhưng bấm gửi duyệt sẽ báo "chưa có
liên kết"**: `CREATE_CUSTOMER`, `CUSTOMER_MASTER_CHANGE`, `SERVICE_PRICE_OVERRIDE`,
`SELL_CARE_PACKAGE`, `CARD_REPRINT`.

> Đây **không phải lỗi lập trình** — là việc **khai báo dữ liệu**, làm được hoàn toàn trên
> giao diện sau khi có Nhóm 1. Nhưng cần anh quyết **ai duyệt cấp nào** cho từng quy trình,
> vì đó là quyết định nghiệp vụ chứ không phải kỹ thuật.

### 2.3. Điều kiện mới dùng được ở mức "chọn quy trình nào"

Bộ đánh giá điều kiện (Nhóm 2) quyết định **phiên bản quy trình nào áp dụng**. Nó **chưa**
làm được:
- Bỏ qua một **bước** theo điều kiện (điều kiện đang ở cấp phiên bản, không phải cấp bước).
- Duyệt song song / biểu quyết nhiều người (engine cố ý chỉ hỗ trợ tuần tự).
- Nhắc hạn, leo thang khi quá hạn (cột `due_duration_minutes` có nhưng chưa có việc chạy nền).

### 2.4. Uỷ quyền khi nghỉ phép

Tài liệu thiết kế có đặc tả bảng uỷ quyền riêng, nhưng hiện chỉ hiện thực **một phần** qua
`Approval_Authorities.delegated_from_user_id` với ngữ nghĩa **THAY THẾ** (người được uỷ
quyền thay hẳn). Khác với đặc tả gốc. Anh đã duyệt hướng THAY THẾ trước đây — ghi lại để
sau này không nhầm là thiếu sót.

---

## 3. 🛑 VIỆC EM TỪ CHỐI LÀM — VÀ VÌ SAO

Ghi lại để anh biết em không im lặng bỏ qua.

### 3.1. Không chuyển `useCompany()` xuống sau nhánh kiểm quyền

Một luồng kiểm tra đề xuất: trong `CustomerCarePackagesSection`, chuyển `useCompany()`
xuống dưới `if (!canView) return null` để component không ném lỗi khi render ngoài provider.

**Em không làm, vì đề xuất đó sai.** `useCompany` và `usePermissions` đều là **hook**. Đặt
hook sau một `return` sớm là **gọi hook có điều kiện** — React sẽ vỡ với *"Rendered fewer
hooks than expected"* ngay khi quyền thay đổi giữa hai lần render. Sửa theo hướng đó **tạo
ra lỗi thật** để vá một vấn đề chỉ tồn tại trong test.

Việc `useCompany` ném lỗi khi thiếu provider là **cố ý và đúng chuẩn**: báo lỗi lắp ráp
thật to còn hơn im lặng hiển thị dữ liệu sai. Trong ứng dụng `CompanyProvider` luôn bọc.

### 3.2. Không khai báo handler bằng bảng cấu hình (phương án A)

Anh chọn phương án C. Em hiện thực hợp đồng handler **bằng mã nguồn**, không bằng bảng, vì:
chuyển trạng thái phải đi qua **phương thức nghiệp vụ** của thực thể (`MarkApproved`,
`SetApproved`) — nơi giữ bất biến và dấu vết sửa đổi. Khai báo bằng bảng rồi ghi thẳng vào
cột trạng thái sẽ **vượt qua toàn bộ bất biến đó**.

Nếu anh vẫn muốn bảng cấu hình, em làm được — nhưng cần anh xác nhận đã hiểu đánh đổi.

### 3.3. Không tự tạo Pull Request, không tự đăng nhập

Theo ràng buộc đã thống nhất: em không dùng thông tin đăng nhập, không tự tạo PR, không tự
đăng nhập vào ứng dụng. Mọi bước cần tài khoản đều do anh thực hiện.

---

## 4. 📌 GHI CHÚ KỸ THUẬT DỄ QUÊN

1. **`npm run build` biên dịch CẢ file test** (`tsc -b && vite build`). Nên trước khi gộp về
   `main` **bắt buộc chạy `npm run build`**, không được chỉ tin `tsc --noEmit` (lệnh này
   thường bị lọc bỏ dòng test nên không thấy lỗi). Đã suýt làm hỏng build của `main` vì việc
   này một lần.

2. **Test "đóng băng phạm vi".** Trong `workflowApi.test.ts` có các test kiểu
   `expect('deleteApproverRule' in mod).toBe(false)` — chúng ghi lại *"giai đoạn này chưa
   xây"*, **không phải cấm thiết kế**. Gặp loại này phải **đối chiếu tài liệu** trước khi phá.
   Guard cho `createCondition`/`deleteCondition` đã được gỡ ở Nhóm 2 vì bộ đánh giá đã có.

3. **Backend trả UTC nhưng thiếu hậu tố `Z`** → JS hiểu nhầm thành giờ local, lệch 7 tiếng.
   Frontend dùng `src/utils/datetime.ts` để chèn `Z` trước khi đổi sang giờ VN. Đã có unit
   test riêng cho tiện ích này.

4. **Restart backend là mất phiên đăng nhập** — lần nào cũng phải đăng nhập lại.

5. **Chạy test frontend nên dùng `--no-file-parallelism`** nếu máy đang tải nặng, hoặc chấp
   nhận chờ. Timeout đã nới lên 20s để hết fail nhấp nháy.

---

## 5. ✅ NHỮNG GÌ ĐÃ XONG (tóm tắt)

Trả lời câu hỏi gốc của anh — *"quy trình chưa linh động, vẫn phải code, tổng quát lên được không?"*:

| Nguyên nhân | Đã xử lý |
|---|---|
| Chưa khai báo cấu hình (~70%) | Nhóm 1 — nhân bản phiên bản, sửa/xoá luật duyệt, bộ chọn thay vì gõ ID, trang tất cả hồ sơ |
| Thiếu bộ đánh giá điều kiện (~25%) | Nhóm 2 — khai báo *"tiền > X thì thêm cấp duyệt"* không cần code |
| Handler cần C# (~5%) | Nhóm 3 — còn ~40 dòng khai báo (giảm 48%) |
| *(phát sinh)* 5 lỗ hổng "hỏng âm thầm" | Nhóm 0 — quy trình in thẻ bị liệt, thiếu handler chết lặng, gói bị từ chối kẹt mãi, liên kết trùng chọn bừa, thiếu cấu hình thì tự duyệt |
| *(phát sinh)* 99 test frontend hỏng sẵn | Đã dọn về 0; bộ test nay xanh nên mọi fail từ giờ là tín hiệu thật |
