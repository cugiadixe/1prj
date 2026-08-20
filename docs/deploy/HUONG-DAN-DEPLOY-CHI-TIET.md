# Hướng dẫn chạy deploy production — từng bước

Dành cho anh Bách chạy trực tiếp. Làm **lần lượt** từng bước, xong bước nào chắc bước đó.

---

## Bước 0 — Chuẩn bị (làm 1 lần)

1. **Mở Docker Desktop** trên máy, đợi nó báo "Engine running" (icon cá voi ở khay hệ thống).
2. **Mở PowerShell** đúng thư mục dự án:
   - Bấm nút Start → gõ `PowerShell` → Enter.
   - Gõ lệnh sau rồi Enter để vào thư mục dự án:
     ```powershell
     cd C:\Projects\PTKD-ERP
     ```
3. **Tạo file `.env`** (chứa mật khẩu DB production). Gõ nguyên khối sau, thay `<MẬT_KHẨU_SA_THẬT>` bằng mật khẩu `sa` thật của container database rồi Enter:
   ```powershell
   @"
   DB_SA_PASSWORD=<MẬT_KHẨU_SA_THẬT>
   DB_NAME=PTKD_PROD
   ASPNETCORE_ENVIRONMENT=Production
   FRONTEND_PORT=80
   BACKEND_PORT=8080
   CORS_ORIGIN=http://localhost
   "@ | Set-Content -Encoding utf8 .env
   ```
   > ❗ Nếu không nhớ mật khẩu `sa`: đó là mật khẩu đã đặt khi lần đầu dựng container database. Không có nó thì migrator/backend sẽ báo *Login failed*. Nếu quên hẳn, nhắn em để tính cách khác (không nên đoán bừa).

---

## Bước 1 — Lấy code mới nhất

```powershell
git checkout main
git pull origin main
```
- **Kỳ vọng:** dòng cuối hiện `... -> main` và không báo lỗi. Có thể thấy `Updating ... 04ba21e`.
- Nếu báo *"local changes would be overwritten"*: dừng lại, nhắn em (đừng ép).

---

## Bước 2 — Áp migration vào DB production (PTKD_PROD)

```powershell
docker compose --profile migrate run --rm migrator
```
- Việc này chỉ thêm các thay đổi CSDL còn thiếu (trong đó có cột `pricing_basis`). Chạy lại nhiều lần cũng an toàn.
- **Kỳ vọng:** log chạy các migration rồi container tự thoát, quay lại dấu nhắc, **không có dòng ERROR/Login failed**.
- Nếu thấy **Login failed for user 'sa'** → sai mật khẩu ở `.env` (quay lại Bước 0.3).

---

## Bước 3 — Build lại ứng dụng (backend + frontend)

```powershell
docker compose build backend frontend
```
- Lệnh này **lâu nhất** (vài phút, lần đầu có thể 5–10 phút). Cứ để chạy.
- **Kỳ vọng:** kết thúc bằng các dòng `naming to ... ptkd-erp-backend` / `ptkd-erp-frontend`, không có `ERROR`.

---

## Bước 4 — Khởi động ứng dụng (HTTPS)

```powershell
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```
- Overlay `docker-compose.prod.yml` bật **HTTPS cổng 443** (cổng 80 tự chuyển hướng sang https), và **ẩn cổng db/backend khỏi host** (an toàn hơn).
- **Kỳ vọng:** hiện 3 dòng `Started` / `Running` cho `db`, `backend`, `frontend`.
- ❗ **Điều kiện:** phải có sẵn `certs/fullchain.pem` + `certs/privkey.pem`. Nếu chưa có (máy mới), sinh trước bằng:
  ```bash
  bash scripts/gen-prod-cert.sh
  ```
  Chi tiết cài CA lên máy nhân viên + gia hạn: xem `docs/deploy/https-cert-noi-bo.md`.
- (Muốn chạy tạm HTTP:80 không SSL thì bỏ `-f docker-compose.prod.yml`.)

---

## Bước 5 — Kiểm tra

```powershell
docker compose ps
```
- **Kỳ vọng:** cả `db`, `backend`, `frontend` đều cột STATUS là `Up` (db là `Up (healthy)`).
- Sao chép nguyên bảng kết quả này **gửi cho em** để em soi giúp.

Rồi mở trình duyệt vào **http://localhost** → đăng nhập → kiểm tra:
- **Trang chủ** hiện dashboard biểu đồ.
- **Bán gói chăm sóc** / **Tạo dịch vụ** có ô chọn khách theo tên.

---

## Nếu có trục trặc — xem log
```powershell
docker compose logs --tail 50 backend
docker compose logs --tail 50 frontend
```
Chụp/copy phần lỗi gửi em, đừng tự sửa cấu hình sâu.

## Muốn hoàn tác (rollback)
```powershell
docker compose down          # tắt app (DB giữ nguyên dữ liệu)
```
Rollback CSDL chỉ làm khi thật cần và sau khi đã tắt app — theo mục 6 trong `RELEASE-2026-08-17-care-package-dashboard.md`.
