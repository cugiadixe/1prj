# Hướng dẫn: Quyền vô hiệu/khoá tài khoản & chốt an toàn

> Cập nhật 2026-08-16. Áp dụng cho mô-đun Bảo mật (Tài khoản) và Tổ chức (Người dùng).

## 1. Quyền mới: `SECURITY_ACCOUNT_DISABLE`

"Vô hiệu / khoá tài khoản người khác" nay là **một quyền RIÊNG** trong ma trận phân quyền,
tách khỏi `SECURITY_ACCOUNT_MANAGE`.

| Mã quyền | Ý nghĩa |
|---|---|
| `SECURITY_ACCOUNT_MANAGE` | Xem/quản lý tài khoản: liệt kê, tạo, kích hoạt, mở khoá, đặt lại mật khẩu, thu hồi phiên… |
| `SECURITY_ACCOUNT_DISABLE` | **Vô hiệu (disable) hoặc khoá (lock) tài khoản người khác.** Quyền nhạy cảm, bắt buộc nhập lý do. |

**Cấp ở đâu:** Bảo mật → **Phân quyền** → chọn người dùng → "Cấp quyền" → tìm
*"Vô hiệu/khoá tài khoản người khác (SECURITY_ACCOUNT_DISABLE)"* trong ô "Quyền cần cấp".
Quyền này hiển thị sẵn (danh mục động), có nhãn **Quyền nhạy cảm** + **Bắt buộc lý do**.

## 2. Luật vô hiệu/khoá (cả 3 điều kiện đều phải đúng)

Để vô hiệu/khoá **một tài khoản**, người thao tác phải:

1. **Có quyền `SECURITY_ACCOUNT_DISABLE`** (nếu không → 403 "không có quyền").
2. **Không phải chính mình** — không ai tự vô hiệu/khoá tài khoản của mình
   (lỗi `AUTH_CANNOT_MODIFY_SELF`). Giao diện cũng ẩn nút Vô hiệu/Khoá trên dòng của chính mình.
3. **Đối tượng không phải quản trị bảo mật CUỐI CÙNG** — hệ luôn phải còn ≥1 người
   đang hoạt động giữ `SECURITY_ADMIN_MANAGE` (lỗi `AUTH_LAST_SECURITY_ADMIN`).

Áp cho cả hai đường: **Bảo mật → Tài khoản** (Vô hiệu/Khoá) và **Tổ chức → Người dùng**
(đổi trạng thái việc làm/tài khoản về khác ACTIVE).

## 3. ⚠️ Lưu ý khi tạo người quản trị mới

Vì "disable" nay là quyền riêng: nếu chỉ cấp `SECURITY_ACCOUNT_MANAGE` mà **quên**
`SECURITY_ACCOUNT_DISABLE`, người đó **quản lý được tài khoản nhưng KHÔNG vô hiệu/khoá được ai**.
→ Khi lập một quản trị viên tài khoản đầy đủ, **cấp kèm cả hai quyền**.

(Các tài khoản đang có `SECURITY_ACCOUNT_MANAGE` tại thời điểm nâng cấp đã được cấp tự động
`SECURITY_ACCOUNT_DISABLE` qua migration V0040 — không ai mất khả năng đang có.)

## 4. Ngừng hoạt động Công ty / Phòng ban (chuỗi phụ thuộc)

Không ngừng được nếu còn ràng buộc đang hoạt động (hệ TỪ CHỐI, không cascade âm thầm):

- **Phòng ban** — chặn nếu còn: phòng con hoạt động, phân công người dùng, hoặc là **phòng chính** của ai đó.
- **Công ty** — chặn nếu còn: công ty con hoạt động, phòng ban hoạt động, hoặc phân công người dùng.

→ Trình tự đúng: gỡ/di chuyển người dùng khỏi phòng ban → ngừng phòng ban → ngừng công ty.
`is_active = false` **không** cắt đăng nhập và **không** thu quyền — chỉ là cờ trạng thái và
ẩn khỏi các ô chọn "đang hoạt động"; bật "Kích hoạt" lại là khôi phục.
