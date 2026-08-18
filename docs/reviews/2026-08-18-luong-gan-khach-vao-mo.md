# Review luồng "gán khách hàng vào mộ"

> Ngày review: **2026-08-18** · Người review: AI (đọc trực tiếp toàn bộ mã luồng) · Trạng thái: **chỉ review, chưa sửa gì**
>
> Phạm vi: luồng thêm người an táng (cốt) vào mộ, gán chủ mộ, và chủ mộ qua đời — nhánh `feature/permission-scope-redesign`.

---

## 1. Luồng hiện tại thực sự có 3 nhánh riêng

| Nhánh | Là gì | Có nối tới `Customer` không? |
|---|---|---|
| **Chủ mộ (owner)** | `OwnerCustomerId` — khách (còn sống) sở hữu/mua mộ | ✅ Có, chọn từ hồ sơ khách |
| **Người an táng (cốt / occupant)** | Người đã mất nằm trong mộ | ❌ **KHÔNG** — chỉ gõ tên tự do |
| **Chủ mộ qua đời** | `ProcessOwnerDeathAsync` — đánh dấu chủ DECEASED + chuyển cho người thừa kế | ✅ Có |

**Điểm mấu chốt:** cái thường gọi "gán khách hàng vào mộ" (thêm **cốt**) hiện **không hề gắn với bản ghi khách hàng** — chỉ nhập tên tự do.

Đường đặt trạng thái `DECEASED` cho khách hiện có 2 nơi, và **không** có nơi "thành cốt":
- Tạo khách với tình trạng "Đã mất" (tính năng mới, `CustomerService.CreateCustomerAsync`).
- Chủ mộ qua đời (`GraveService.ProcessOwnerDeathAsync`).

---

## 2. Phát hiện (xếp theo mức độ)

### 🔴 CAO-1 — Cốt tách rời hồ sơ khách hàng (`DeceasedCustomerId` là cột chết)

- `GraveOccupant.DeceasedCustomerId` có setter **private**, constructor không nhận, không method nào ghi — `GraveOccupant.cs:9`.
- `AddOccupantAsync` tạo cốt từ **FullName tự do**, không set link — `GraveService.cs:452`.
- `CreateGraveOccupantRequest` không có trường customerId — `GraveDtos.cs:167`.
- Modal "Thêm người an táng" ở FE **không có ô chọn khách** — `GraveDetailPage.tsx:598`.
- Trớ trêu: FE **đã dựng sẵn** link "Xem khách hàng (đã mất)" khi có `deceasedCustomerId` — `GraveDetailPage.tsx:283` và `:703` — nhưng backend không bao giờ ghi field đó ⇒ link gần như không bao giờ hiện (chỉ dữ liệu seed mới có).

**Hệ quả:** khách tạo với "Đã mất" và cốt trong mộ là **hai dữ liệu rời**, không đồng bộ. Không có cách nào trong app đưa một khách đã có vào mộ như một cốt gắn hồ sơ.

### 🔴 CAO-2 — Không kiểm sức chứa mộ (`CotCount`)

`AddOccupantAsync` không giới hạn số cốt theo `CotCount` — `GraveService.cs:436-477`. Mộ đơn (1 cốt) vẫn thêm được vô hạn cốt, trái định nghĩa đơn/đôi/gia tộc — `Grave.cs:13-19`.

### 🔴 CAO-3 — Thêm cốt không đổi trạng thái mộ

`AddOccupantAsync` không chuyển `Status` EMPTY→OCCUPIED. Phải sửa tay qua `UpdateGrave` ⇒ dễ lệch (mộ có cốt nhưng vẫn "còn trống").

### 🟠 TRUNG-4 — Rederivation quan hệ khi đổi chủ là nhánh chết

Khi chuyển quyền, đoạn tái suy diễn nhãn quan hệ của cốt lọc theo `DeceasedCustomerId != null` — `GraveService.cs:711` và `:756`. Do CAO-1, cốt tạo qua app luôn null ⇒ **không cốt nào được tái suy diễn**; nhãn quan hệ giữ nguyên (sai) sau khi đổi chủ.

### 🟠 TRUNG-5 — Gán chủ mộ không kiểm phạm vi công ty của khách

`EnsureOwnerExistsAsync` chỉ kiểm khách **tồn tại**, không kiểm cùng công ty/scope — `GraveService.cs:939-943`; áp cho CreateGrave (`:320`), UpdateGrave (`:397`), Transfer (`:738`). Request thủ công có thể gán chủ là khách **công ty khác**. Nghĩa trang đã scope chặt nhưng chủ thì chưa ⇒ cần chốt là cố ý hay lỗ hổng.

---

## 3. Kết luận riêng: "tạo khách Đã mất" ↔ "cốt trong mộ"

Hai tính năng **liên quan nhưng chưa nối nhau**. Đúng kỳ vọng "gán khách vào mộ" thì trong modal thêm cốt phải cho **chọn một khách hàng** ⇒ set `DeceasedCustomerId`, (tùy chọn) tự set khách thành `DECEASED`, đổi `Status` mộ, và kiểm sức chứa. Hiện tại chưa có mắt xích này.

---

## 4. Những chỗ ĐÃ TỐT (không cần sửa)

- **Scope công ty trên mộ rất chắc**: mọi thao tác occupant/transfer đều qua `EnsureGraveAccessibleAsync` — `GraveService.cs:450`, `:496`, `:733`.
- **Quyền tách bạch**: occupant đòi `GRAVE_UPDATE`, chuyển quyền đòi `GRAVE_TRANSFER_OWNERSHIP` — `GravesController.cs:81`, `:128`.
- **Giao dịch Serializable + RowVersion** cho update/transfer/owner-death; **audit đầy đủ** + `ThrowIfContainsSensitiveData`.
- `ProcessOwnerDeath` **chặn trước theo công ty** để tránh trạng thái nửa vời — `GraveService.cs:808-822`.

---

## 5. Đề xuất ưu tiên

1. **Nối cốt ↔ khách hàng** (thêm ô chọn khách trong modal thêm cốt → set `DeceasedCustomerId`): làm sống lại link FE + rederivation + đồng bộ trạng thái, và đúng thứ nghiệp vụ mong. *Hạng mục lớn, nên chốt hướng trước.*
2. **Kiểm sức chứa `CotCount`** khi thêm cốt.
3. **Tự đổi `Status` mộ** EMPTY→OCCUPIED khi có cốt (và RELOCATED khi bốc).

Điểm cần anh quyết cho mục #1 nếu triển khai:
- Chọn khách là **bắt buộc** hay **tùy chọn** (cho phép cốt "vãng lai" chỉ có tên)?
- Khi gán cốt là một khách, có **tự set khách đó thành `DECEASED`** không?
- Có **kiểm khách cùng công ty** với mộ không?

---

## 6. Phương án khắc phục (đã chốt 2026-08-18)

### 6.1 Quyết định của chủ dự án (anh Bách)

| # | Quyết định | Ghi chú triển khai |
|---|---|---|
| D1 | **Bắt buộc quan hệ GIA ĐÌNH** giữa người chết ↔ chủ mộ để đặt cốt | "Gia đình" theo nghĩa **rộng** (mẹ, vợ/chồng, bên ngoại, dâu/rể…), **KHÔNG** giới hạn cùng họ. Người không có cạnh gia đình ⇒ không đặt được (chấp nhận). |
| D2 | Người chết là **bản ghi Customer** | Occupant bắt buộc có `DeceasedCustomerId`; bỏ nhập tên tự do. |
| D3 | Ép **sức chứa**: số cốt đang nằm ≤ `CotCount` | Mộ 4 cốt ⇒ tối đa 4 người **đang hiệu lực**. |
| Q2 | Một người **một suất tại một thời điểm**; hỗ trợ **bốc/cải táng** | Ràng buộc theo suất ĐANG hiệu lực; bốc ⇒ suất RELOCATED ⇒ giải phóng người + chỗ. |
| D4 | Người thừa kế lấy từ **liên hệ khẩn cấp** (theo ưu tiên) | Liên hệ khẩn cấp nên là một cạnh trong đồ thị gia đình. |
| D5 | Xây **UI khai quan hệ gia đình** (đường nạp cạnh còn thiếu) | Tiền đề bắt buộc, làm trước. |

### 6.2 Hạ tầng ĐÃ CÓ (tái dùng, không xây lại)

- Bảng đồ thị quan hệ: `CustomerRelationships` (V0022), `RelationshipKinds`, `KinshipCompositions`.
- Engine suy nhãn 2 chiều (direct + 2 bậc): `RelationshipDerivationService.DeriveOwnerToOccupantsAsync`.
- FE đã có `deriveInverseLabel` + `OccupantRelationshipFields`.
- **Lỗ hổng cốt lõi:** đồ thị **chỉ được ĐỌC**, không nơi nào GHI `CustomerRelationships` (không endpoint/UI/service) ⇒ D5 là bắt buộc; và occupant chưa gắn `DeceasedCustomerId`.

### 6.3 Luồng đã chốt (state machine)

1. **Chủ mộ (sống) + tạo mộ** với số cốt `CotCount` (1..n) → mộ EMPTY.
2. **Khai quan hệ gia đình** (owner ↔ người thân) — ghi cạnh `CustomerRelationships`.
3. Ai mất → đánh dấu **DECEASED** (tạo mới "Đã mất" hoặc đánh dấu khách hiện có).
4. **Đặt cốt**: danh sách ứng viên = người DECEASED **có đường quan hệ gia đình thật tới chủ (direct/2-hop, KHÔNG tính fallback `Other`) + chưa có suất đang hiệu lực**. Khi đặt: set `DeceasedCustomerId`, suy & lưu nhãn 2 chiều, **kiểm sức chứa**, đổi status mộ → OCCUPIED.
5. **Bốc/cải táng**: suất → RELOCATED ⇒ giải phóng người + chỗ; status mộ cập nhật lại.
6. **Chủ chết**: DECEASED → chọn thừa kế từ liên hệ khẩn cấp (ưu tiên, có xác nhận) → chuyển quyền → (tùy chọn) đặt chủ cũ làm cốt (quan hệ theo chủ mới).

### 6.4 Thay đổi dữ liệu cần làm

- `GraveOccupant`: `DeceasedCustomerId` **bắt buộc** + method set; thêm **trạng thái suất** (ACTIVE/RELOCATED); ràng buộc **duy nhất theo suất ACTIVE** cho mỗi customer.
- Ép **sức chứa** khi thêm cốt (đếm suất ACTIVE ≤ `CotCount`).
- **Tự chuyển status mộ** EMPTY→OCCUPIED (và về EMPTY/RELOCATED khi bốc hết).
- Đường **GHI `CustomerRelationships`** (service + endpoint + UI) — mã quyền mới cho khai quan hệ.
- Liên hệ khẩn cấp gắn/kèm cạnh quan hệ để dùng làm nguồn thừa kế.
- Nối FE modal "Thêm người an táng": **chọn khách hàng** (không gõ tên tự do), nhãn quan hệ suy từ đồ thị.

### 6.5 Lộ trình triển khai (đề xuất theo thứ tự phụ thuộc)

- **P1 — Đồ thị quan hệ (D5):** entity write path + endpoint + UI khai/sửa/xoá quan hệ owner↔người thân + mã quyền. *(tiền đề)*
- **P2 — Nối cốt ↔ khách + sức chứa + status:** `DeceasedCustomerId` bắt buộc, filter ứng viên (quan hệ + chưa có suất), kiểm CotCount, tự đổi status mộ, suy nhãn 2 chiều. Migration schema occupant.
- **P3 — Bốc/cải táng:** trạng thái suất RELOCATED + giải phóng.
- **P4 — Chủ chết → thừa kế từ liên hệ khẩn cấp:** nâng `ProcessOwnerDeathAsync` lấy heir từ emergency contacts, đặt chủ cũ làm cốt.
- **P5 — Vá TRUNG-5:** kiểm phạm vi công ty của chủ mộ khi gán/chuyển.

> Mỗi P có migration + build + kiểm chứng riêng; production do owner chạy. Chưa bắt đầu code khi chưa chốt điểm khởi động.

---

## Phụ lục — Các file/điểm vào đã đọc

- Backend service: `src/backend/PTKD.Application/Graves/Services/GraveService.cs` — `AddOccupantAsync` (436), `UpdateOccupantAsync` (479), `TransferOwnershipAsync` (698), `ProcessOwnerDeathAsync` (803), `EnsureOwnerExistsAsync` (939).
- Domain: `Grave.cs`, `GraveOccupant.cs`.
- DTO: `Graves/DTOs/GraveDtos.cs`, `TransferOwnershipDtos.cs`.
- API: `PTKD.Api/Controllers/GravesController.cs`.
- Scope: `Security/Authorization/GraveCompanyScope.cs`.
- Frontend: `src/frontend/src/graves/GraveDetailPage.tsx`, `OccupantRelationshipFields.tsx`, `gravesApi.ts`, `types.ts`.
- Migration liên quan: `database/migrations/V0017,V0019,V0020,V0021`.
