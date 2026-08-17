# KẾ HOẠCH: In thẻ mộ (template động + watermark) + Duyệt in lại

> **NHÁP THẢO LUẬN** — chưa phải hồ sơ trình duyệt (Hiến pháp điều 3-4). Soạn 2026-08-16.
> Bám mã nguồn thật; 4 trụ cột đã kiểm chứng đối kháng trên DB `PTKD_DEV` + source (mục 7).

## 0-BIS. QUYẾT ĐỊNH ĐÃ CHỐT (anh Bách, 2026-08-16)

| # | Câu | Quyết |
|---|---|---|
| 1 | Mở lại OD-1B8-012 cho KHỐI B | **Đồng ý** (⚠️ cần ghi thành quyết định có mã để "đã duyệt = hiệu lực" — AI soạn nháp bản ghi) |
| 2 | In cả nền hay overlay phôi | **Overlay lên phôi vàng in sẵn** — và **hệ thiết kế PHÔI master** (khung+chữ+watermark) để xuất PDF cho xưởng in hàng loạt |
| 5 | Con dấu + chữ ký P.GĐ | **Để trống, ký/đóng dấu tay sau in** → bỏ SealAsset/SignatureAsset khỏi KHỐI B |
| 6 | CCCD chủ mộ | **In đủ** (thẻ của chính chủ mộ; vẫn ghi vết ai in theo NĐ13) |

**Hệ quả kiến trúc:** KHỐI B có **HAI đường render dùng chung một hệ toạ độ**:
- **(1) Phôi master:** render TRỌN (nền + khung + 4 điều lưu ý + **watermark** + vùng ký/dấu để trống), cấu hình theo công ty/nghĩa trang → xuất PDF gửi xưởng in ra giấy vàng hàng loạt. Đây là nơi "thiết kế động + watermark" áp dụng.
- **(2) Overlay dữ liệu:** render CHỈ dữ liệu biến đổi tại đúng toạ độ, `render_background=false` + offset canh lề mm → in đè lên phôi in sẵn khi cấp thẻ.

Còn chờ anh quyết: câu 3 (khổ giấy mm), 4 (thư viện PDF), 7 (phí in lại), 8 (đếm theo thẻ), 9 (scope template), 10 (duyệt publish template).

---

## 0. KẾT LUẬN TRƯỚC

1. **Nền "duyệt in lại" đã có ~70%, nhưng chức năng IN THẬT = 0%.** Máy trạng thái, workflow engine, handler, controller, quyền — đã có. Render/PDF/template/watermark **hoàn toàn chưa tồn tại**, phải xây mới.

2. **Luật "in lần đầu miễn duyệt, lần 2+ mới duyệt" HIỆN KHÔNG CHẠY** — vì 3 lỗ nền:
   - `Card.IncrementPrintCount` là **code chết** (`Card.cs:38`, không nơi nào gọi) → `PrintCount` vĩnh viễn = 0 → mọi yêu cầu luôn bị phân loại `INITIAL_PRINT` (`CardReprintRequestService.cs:45`).
   - **Chưa seed binding/definition** cho `CARD_REPRINT` (DB dev: 0 definition, 0 binding) → bấm Submit sẽ **văng `WF_NO_VALID_BINDING`**.
   - `SubmitAsync` **luôn** mở workflow bất kể loại in (`CardReprintRequestService.cs:96-107`); chưa có nhánh bỏ duyệt cho lần đầu.

3. **CHẶN QUẢN TRỊ:** quyết định **OD-1B8-012** (`docs/architecture/phase-1b8a-project-owner-blocker-decision-response.md:98-114`) **HOÃN tường minh** việc sinh template/PDF động — *"remains deferred unless a separate PO scope acceptance explicitly authorizes it"*. Không kết tinh hồ sơ trình duyệt cho khối render cho tới khi anh Bách cấp *scope acceptance* mới. Xây trước = trái Hiến pháp điều 4-5.

4. **Chia 2 KHỐI tách bạch giá trị:**
   - **KHỐI A — Duyệt in lại (giao được NGAY):** sửa 3 lỗ nền + bật engine duyệt. Nằm **trong** ranh giới MVP mà OD-1B8-012 cho phép (record/approve/reject/mark-printed). Rẻ, không chờ quyết định mới.
   - **KHỐI B — Render + template + watermark:** cần anh Bách **mở lại OD-1B8-012** trước. Khối lớn (engine PDF, font VN, mô hình template, kho asset).

5. **Chốt kỹ thuật (KHỐI B):** engine render = **PDF native server-side** (mặc định **PdfSharp/MigraDoc**, giấy phép MIT; QuestPDF chỉ nếu anh duyệt ngân sách bản quyền cho tập đoàn). Template = **Mức 1** (layout cố định trong mã, cấu hình nội dung + thương hiệu + watermark theo công ty/nghĩa trang), cài sẵn `LayoutKey` để nâng cấp sau.

---

## 1. HIỆN TRẠNG (ĐÃ CÓ vs THIẾU)

### ĐÃ CÓ (dùng lại được)

| Hạng mục | File:line | Ghi chú |
|---|---|---|
| Máy trạng thái in lại đầy đủ | `CardReprintRequest.cs:10-140` | DRAFT→PENDING_APPROVAL→APPROVED→PENDING_PAYMENT→PAID→PRINTED→RELEASED, mỗi transition có guard |
| Phân biệt loại in ở tầng dữ liệu | `CardReprintRequestService.cs:45-46` | `requestType = PrintCount==0 ? INITIAL : REPRINT` |
| Controller REST + phân quyền công ty | `CardReprintRequestsController.cs:26-124` | create/submit/approve/reject/create-payment/mark-printed/mark-released |
| Handler thực thi workflow (tự đăng ký) | `CardReprintExecutionHandler.cs` | ProcessCode `CARD_REPRINT`, duyệt xong `SetApproved` |
| **Khuôn mẫu vàng cần mô phỏng** | `CarePackageRequestService.cs:194-205` | `IsApprovalRequiredAsync` → bỏ qua duyệt khi không cần (đã chạy thật) |
| Cổng quyết định engine | `WorkflowRuntimeService.cs:809-842` | Chưa cấu hình → trả **null** (không thoát duyệt âm thầm) |
| Danh mục process CARD_REPRINT | `V0035...sql:22` | Đã seed process, **chưa** seed definition/version/binding |
| Hạ tầng lưu ảnh + ImageSharp | `GraveFileStorage.cs` | Lưu file trên đĩa, sinh thumbnail; dùng lại cho asset watermark |
| Khuôn admin có version/binding/whitelist | `WorkflowConfigurationController.cs`, `WorkflowDefinitionVersion.cs`, `WorkflowBinding.cs`, `WorkflowConditionField.cs` | Bản mẫu cho mô hình template |
| Nguồn dữ liệu tự điền thẻ | `GraveOccupant.cs`, `Grave.cs`, `Profile.cs`, `Cemetery.cs`, `Company.cs` | Đủ phần lớn 4 mặt thẻ |

### THIẾU (phải làm mới)

- **Chức năng in thật = 0:** không engine render, không PDF/HTML template, không watermark, không preview. `MarkPrintedAsync` chỉ đổi trạng thái (`CardReprintRequestService.cs:209-231`).
- **3 lỗ nền của luật "lần 2 mới duyệt"** (mục 0.2).
- **Không có CardsController / luồng tạo thẻ + cấp SỐ THẺ** trong production — `Card` chỉ tạo trong test.
- **Không có đường in miễn duyệt/miễn phí:** `SetPrinted` bắt buộc từ `StatusPaid` (`CardReprintRequest.cs:122`).
- **Card mỏng:** thiếu `card_number` (SỐ THẺ 5180), `issue_date`, "Mẫu", "Số 56/2020" (`Card.cs:11-20`).
- **Card↔Grave nối mong manh:** `Card.GraveId` là chuỗi `grave_code`, không FK, không khớp `Grave.Id` (long).
- **Không có kho cấu hình text tĩnh + watermark** theo công ty/nghĩa trang (tên XN, 4 điều "QUÝ KHÁCH LƯU Ý", SĐT, dấu, chữ ký P.GĐ).
- **Không có font tiếng Việt nhúng** — server-render sẽ mất dấu nếu không bundle `.ttf`.
- **Payload workflow thiếu trường phân loại:** chỉ `{CardId, ReasonCode}` (`CardReprintRequestService.cs:102`).

---

## 2. KIẾN TRÚC ĐỀ XUẤT

### 2.1 Engine render → PDF native server-side (KHỐI B)
Thẻ là **văn bản pháp lý** ("thẻ thay hợp đồng") + chứa **CCCD** (NĐ13) + cần **bản in lưu vết** để kiểm toán reprint → buộc phải có artifact tất định, phân quyền tải, in nhất quán.
- **Loại HTML `@media print`:** Chrome mặc định TẮT in nền → watermark có thể không in; lề/scale lệch theo máy; không sinh artifact kiểm toán. (Chỉ dùng làm preview = mở PDF inline.)
- **Loại headless Chromium:** quá nặng vận hành trên Windows production cho một tấm thẻ.
- **Thư viện:** mặc định **PdfSharp/MigraDoc (MIT)**. **QuestPDF** chỉ khi anh duyệt ngân sách (Community giới hạn theo doanh thu; INDEVCO là tập đoàn nên gần như phải mua Professional — **cần xác minh**).
- **Bắt buộc:** nhúng font Unicode đủ dấu tiếng Việt vào repo.
- **Hai đường dùng chung hệ toạ độ (đã chốt câu 2):** `render_background=true` → **phôi master** (nền + khung + chữ cố định + watermark + vùng ký/dấu để trống) xuất PDF cho xưởng in. `render_background=false` → **overlay dữ liệu** tại đúng toạ độ + offset mm để in đè lên phôi in sẵn. Định nghĩa toạ độ MỘT lần, hai lớp (tĩnh/dữ liệu) cùng dùng → phôi và bản in đè luôn khớp ô.
- **Bỏ khỏi scope (câu 5):** không lưu/không in ảnh con dấu + chữ ký P.GĐ — để trống, ký/đóng dấu tay sau in.

### 2.2 Mô hình template + watermark → Mức 1 + mối nối mở rộng
Layout cố định trong mã (bố cục 4 mặt gần như bất biến). Cái *động* là: (a) **dữ liệu tự điền** qua whitelist field, (b) **text tĩnh + thương hiệu + watermark** cấu hình theo công ty/nghĩa trang. Cài sẵn `LayoutKey` (`GRAVE_CARD_4PANEL_V1`) để thêm layout sau mà không đập schema.
- **Versioning DRAFT→PUBLISHED→ACTIVE→RETIRED** (khuôn `WorkflowDefinitionVersion`): không sửa tại chỗ bản ACTIVE.
- **Whitelist field** (khuôn `WorkflowConditionField`): DEV khai trước qua migration, admin chỉ bật/tắt — chặn rò rỉ dữ liệu nhạy cảm; đánh dấu `IsSensitive` cho CCCD.

### 2.3 Đếm in + duyệt in lại → chặn ở tầng service, luật cấu hình được (KHỐI A)
Mô phỏng khuôn CarePackage (`CarePackageRequestService.cs:194-205`, đã chạy thật):
- **Đếm theo THẺ**, ràng buộc "1 mộ = 1 thẻ hoạt động". Nguồn sự thật = bảng mới **`Card_Print_History`** (append-only); `Card.PrintCount` hạ thành cache cập nhật cùng giao dịch.
- **Điểm chặn đặt ở HÀNH ĐỘNG IN**, không phải lúc tạo yêu cầu: sự kiện in chạy trong giao dịch Serializable — đọc số lần in, gán `print_sequence`, **tái phân loại** INITIAL/REPRINT tại đó. Đây là chỗ khoá lỗ "hai lần in đầu song song cùng lọt miễn duyệt".
- **Luồng:** INITIAL → bỏ workflow + bỏ phí → thẳng "sẵn sàng in" → PRINTED. REPRINT → mở instance `CARD_REPRINT` → duyệt → (tùy) phí → in.
- **Cổng quyết định:** `IsApprovalRequiredAsync("CARD_REPRINT", companyId, payload{RequestType, ReprintNumber,...})`; seed 1 binding điều kiện `RequestType EQ REPRINT`. Fallback miền `configuredDecision ?? (RequestType==REPRINT)`.

---

## 3. MÔ HÌNH DỮ LIỆU / MIGRATION (đánh số thủ công, tiếp từ V0039)

### KHỐI A
- **V0039 — nền đếm in + bật luật lần 2:**
  - Bảng `Card_Print_History` (append-only): `id, card_id, company_id, print_sequence, print_type, reprint_request_id?, workflow_instance_id?, printed_by_user_id, printed_at, reason_code, notes`; UNIQUE 1 dòng INITIAL/thẻ.
  - Seed `Workflow_Condition_Fields` cho CARD_REPRINT: `RequestType` (TEXT), `ReprintNumber` (NUMBER).
  - Seed `Workflow_Definition` + `Version` (ACTIVE) + `Step` + `ApproverRule` + `Binding`, điều kiện `RequestType EQ REPRINT` (khuôn `V0031`).
- **Sửa code (không migration):** nối `IncrementPrintCount`; thêm transition bypass `SetReadyToPrint()` cho INITIAL; `CreateRequest` gọi `IsApprovalRequiredAsync`; `Submit` chỉ mở workflow khi cần; thêm `RequestType`+`ReprintNumber` vào payload; `MarkPrinted` ghi history + tăng PrintCount trong giao dịch Serializable + tái phân loại.

### KHỐI B (chỉ sau khi mở OD-1B8-012)
- **V0040 — trường thẻ vật lý:** `Card.card_number`, `issue_date`, `card_template_version_id`; cân nhắc siết `grave_id` thành FK.
- **V0041 — mô hình template (5 bảng, khuôn workflow):** `Card_Templates` (+`LayoutKey`), `Card_Template_Versions` (bất biến; `ContentJson` = text tĩnh + TOẠ ĐỘ vùng dữ liệu, `WatermarkEnabled/Opacity`; **không** SealAsset/SignatureAsset — câu 5), `Card_Template_Bindings` (scope GLOBAL/COMPANY/CEMETERY + Priority + Effective), `Card_Template_Fields` (whitelist + `IsSensitive`), `Card_Template_Assets` (chỉ **WATERMARK** + LOGO). Seed 1 template GLOBAL default.
- Thêm quyền `CARD_TEMPLATE_MANAGE/VIEW` + quyền render/tải thẻ.

---

## 4. LỘ TRÌNH THEO PHA

- **Pha 0 — Quyết định (anh Bách, không code):** mở lại OD-1B8-012 cho KHỐI B + chốt câu hỏi mục 5. *Kiểm chứng: có decision_id.*
- **Pha 1 — KHỐI A: Duyệt in lại chạy đúng (giao NGAY):** sửa 3 lỗ nền + V0039. *Kiểm chứng (test tích hợp):* (a) card mới → INITIAL in thẳng, không workflow; (b) card đã in 1 lần → REPRINT, Submit tạo instance, người đề xuất không tự duyệt; (c) 2 request in đầu song song → chỉ 1 dòng INITIAL; (d) history khớp PrintCount.
- **Pha 2 — Luồng tạo thẻ + số thẻ:** `CardsController`, cấp `card_number` theo năm, siết Card↔Grave. V0040. *Kiểm chứng: sinh số thẻ duy nhất/năm; join kéo đúng dữ liệu.*
- **Pha 3 — KHỐI B: Template + watermark + admin (cần OD-1B8-012):** V0041 + quyền + `CardTemplateController` + FE upload watermark. *Kiểm chứng: publish version → resolve binding CEMETERY > COMPANY > GLOBAL.*
- **Pha 4 — KHỐI B: Engine render PDF + phân quyền tải:** thư viện PDF + font VN; map field → dữ liệu; endpoint tải PDF gác quyền + audit; lưu artifact vào `MarkPrintedAsync`. *Kiểm chứng: đủ dấu tiếng Việt; watermark in ra; in lại = in đúng bản đã duyệt.*
- **Pha 5 — Preview + in overlay lên phôi + calibration:** cờ `render_background` + offset mm; xem trước WYSIWYG.

---

## 5. CÂU HỎI CẦN ANH BÁCH QUYẾT

1. **OD-1B8-012:** anh tái kích hoạt + cấp scope acceptance cho "mẫu in động + watermark" (KHỐI B) chứ? (KHỐI A không cần.)
2. **In cả nền hay chỉ overlay lên phôi giấy vàng in sẵn?** (mặc định `render_background` + có cần offset mm.)
3. **Khổ giấy chính xác** (gập đôi — A5 gập từ A4 hay khổ riêng; dọc/ngang, số mm)?
4. **Thư viện PDF:** duyệt ngân sách QuestPDF, hay bắt buộc MIT (PdfSharp/MigraDoc)?
5. **Con dấu đỏ + chữ ký P.GĐ:** in kèm ảnh trên bản hệ thống (rủi ro trông như đã ký/đóng dấu) hay để trống ký/đóng dấu tay sau in?
6. **CCCD chủ mộ (NĐ13):** in đầy đủ / che một phần / hiện theo quyền người in?
7. **In lại có thu phí không?** Luôn thu, hay tùy lý do (mất thẻ do khách = thu; lỗi in nội bộ = miễn)?
8. **Đếm theo THẺ + "1 mộ = 1 thẻ hoạt động"** — đồng ý? Mất thẻ cấp lại tính REPRINT cộng dồn hay thẻ mới reset?
9. **Phạm vi template + binding duyệt:** GLOBAL hay theo công ty/nghĩa trang? Dùng company theo `Cemetery.CompanyId` (mộ-qua-nghĩa-trang, V0038) đúng chứ?
10. **Có cần duyệt khi PUBLISH template** (thẻ là văn bản pháp lý) hay chỉ cần quyền `CARD_TEMPLATE_MANAGE`?

## 6. RỦI RO / CẠM BẪY

- **Vi phạm quản trị:** xây KHỐI B trước khi mở OD-1B8-012 = trái Hiến pháp 4-5. Pha 0 bắt buộc.
- **Bug nền che luật:** `PrintCount` không tăng là điều kiện tiên quyết — không sửa thì luật "lần 2 mới duyệt" không thể chạy.
- **Kẹt luồng nếu bật nửa vời:** `SetPrinted` bắt buộc `StatusPaid` (`CardReprintRequest.cs:122`) → in lần đầu bị buộc qua phí/văng lỗi nếu quên transition bypass.
- **Quên seed binding:** bật "in lại phải duyệt" mà thiếu binding/version/step → mọi yêu cầu in lại **kẹt cứng** `WF_NO_VALID_BINDING`.
- **Concurrency 2 lần in đầu:** phải gán `print_sequence` + tái phân loại trong giao dịch Serializable + UNIQUE 1 INITIAL/thẻ.
- **Font tiếng Việt:** không bundle `.ttf` → PDF ra ô vuông/mất dấu.
- **Card↔Grave chuỗi grave_code (không FK):** join điền thẻ mong manh → thẻ trống/sai; siết trước Pha 2.
- **Lệch nguồn công ty:** `Card.CompanyId` trỏ thẳng Companies, còn mộ gắn công ty QUA `Cemetery.CompanyId` (V0038) — chốt dùng nguồn nào cho binding + tên đơn vị trên thẻ, tránh xem/ghi chéo công ty.
- **Con dấu/watermark sai scope:** đóng dấu XN này lên thẻ XN khác nếu binding sai. Ảnh con dấu đỏ nhạy cảm (giả mạo) — lưu có kiểm soát quyền, không để thư mục tĩnh công khai.
- **4 điều "QUÝ KHÁCH LƯU Ý" là chữ pháp lý:** admin gõ sai = sai văn bản → cần versioning (có thể bắt buộc duyệt publish — câu 10).
- **Copy-based khi in lại:** PDF đã render phải lưu/đóng phiên bản; in lại = in đúng bản đã duyệt, không render lại âm thầm.

## 7. XÁC MINH ĐỐI KHÁNG (đã tự kiểm, không chỉ tin agent)

| Khẳng định | Cách kiểm | Kết quả |
|---|---|---|
| `IncrementPrintCount` là code chết | grep toàn repo | ✅ chỉ có định nghĩa `Card.cs:38`, 0 lời gọi |
| `SubmitAsync` mở workflow vô điều kiện | đọc `CardReprintRequestService.cs:96-107` | ✅ luôn tạo `ProcessCode=CARD_REPRINT` |
| Chưa seed binding/definition CARD_REPRINT | truy vấn `PTKD_DEV` | ✅ 0 Workflow_Definitions, 0 Workflow_Bindings (chỉ ASSIGN_CARE_PACKAGE có binding) |
| OD-1B8-012 hoãn PDF/template | đọc `phase-1b8a-...md:98-114` | ✅ "remains deferred unless a separate PO scope acceptance" |
