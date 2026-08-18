# Quyết định chủ sở hữu — Đánh giá bảo mật 2026-08-18

> Người quyết định: **Đào Hải Bách** · Phòng CNTT · Mô hình Quản trị Một Chủ (Single-Owner)
> Ngày: **18/08/2026** · Trạng thái: **APPROVED**
> Bối cảnh: đợt đánh giá bảo mật toàn dự án 2026-08-18. Các phát hiện #1–#5, #7, #8 đã được VÁ
> (xem lịch sử commit "Bảo mật: ..."). Tài liệu này chốt hai phát hiện còn lại (#6, #9) vốn là
> **quyết định chính sách**, không phải lỗi — chủ sở hữu chọn **GIỮ NGUYÊN hành vi hiện tại**.

---

## SR-06 — Quyền phạm vi TOÀN CỤC được phép thao tác xuyên công ty (self-check endpoints)

**Chốt:** GIỮ NGUYÊN. Người có grant **GLOBAL** một mã quyền được thao tác trên **mọi công ty**,
kể cả công ty mình không phải thành viên, qua các endpoint tự-kiểm bằng `EvaluateAsync`.

**Hiện trạng kỹ thuật:**
- Đường **attribute** `[RequirePermission(code, PermissionScope.Company)]` áp **hai** chốt:
  `IsMemberOfAsync` (người gọi phải thuộc công ty) **và** `scope.Allows(companyId)`.
- Đường **tự-kiểm** `EvaluateAsync(userId, code, companyId)` chỉ áp `IsGlobal || CompanyIds.Contains(companyId)`,
  **không** kiểm thành viên → grant GLOBAL đi qua được mọi công ty.
- Các controller đi đường tự-kiểm: `PaymentTransactionController`, `ReconciliationController`,
  `ServiceController`, `ServiceTypeController`.

**Lý do giữ:** quyền GLOBAL được cấp cho vai trung tâm (vd kế toán/đối soát cấp tập đoàn) cần thao
tác xuyên công ty; buộc phải là thành viên từng công ty sẽ cản trở vai này. Chốt `Allows(companyId)`
vẫn bảo đảm người chỉ có quyền theo-công-ty **không** vượt sang công ty khác.

**Hệ quả / lưu ý:** hành vi này **không nhất quán** với các module khác (Customer/Grave/Card/
CarePackage) vốn đi đường attribute và **đòi** thành viên. Chấp nhận sự khác biệt này có chủ đích.
Vì grant GLOBAL là quyền nhạy cảm, cấp nó phải cân nhắc kỹ (nó = thao tác được mọi công ty).

**Không đổi code.** Đợt rà bảo mật sau KHÔNG coi đây là lỗ hổng — trỏ về quyết định này.

---

## SR-09 — Xoá mềm payment tiếp tục dùng `PAYMENT_CREATE_DRAFT`

**Chốt:** GIỮ NGUYÊN. Không tạo quyền `PAYMENT_DELETE` riêng.

**Hiện trạng kỹ thuật:**
- `PaymentTransactionController.SoftDelete` (`HttpDelete("{id}")`) kiểm `PAYMENT_CREATE_DRAFT`.
- `PaymentTransaction.SoftDelete()` gọi `EnsureNotConfirmed()` → **payment đã xác nhận KHÔNG xoá
  được**. Xoá mềm chỉ tác động **bản nháp**.

**Lý do giữ:** vì xoá chỉ áp dụng cho nháp (chưa thành bản ghi tài chính chính thức), nguyên tắc
"ai tạo nháp thì xoá được nháp" là hợp lý; rủi ro thấp. Tạo quyền riêng cần seed catalog + cấp lại
cho các vai đang có `CREATE_DRAFT` (rủi ro cấp thiếu → không ai xoá được), lợi ích không tương xứng.

**Không đổi code.** Nếu sau này xoá mềm được mở cho payment đã xác nhận, phải xem lại quyết định này.

---

## Còn treo (không thuộc #6/#9)

- Tạo **login DB ít quyền** thay `sa` cho app (khuyến nghị, chưa làm).
- **Kích hoạt khoá ký JWT bền** ở production (`Jwt:SigningKeyPath`) — code đã sẵn (#7), chưa cấu hình.
