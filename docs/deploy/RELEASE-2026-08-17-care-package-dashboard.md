# Runbook deploy production — 2026-08-17

**Nhánh nguồn:** `main` (tip `cdbeb61`) · **Deploy từ:** `main` · **Cơ chế:** Docker Compose

> AI chỉ soạn runbook + verify. **Bước chạy production do owner (anh Bách) thực thi.**

---

## 1. Nội dung bản phát hành
Đợt này dồn toàn bộ nhánh phát triển về `main` (trước đó `main` lệch ~51 commit). Nhóm chính:

- **Gói chăm sóc:** bán từ danh mục dịch vụ, tính giá theo cốt/theo mộ (`ServiceType.PricingBasis`), phần mộ bắt buộc + lọc theo chủ sở hữu + chặn cứng, cốt tự lấy từ phần mộ.
- **Trang chủ:** dashboard mới 8 biểu đồ (SVG) + KPI, endpoint `GET /dashboard/summary`.
- **Khách hàng:** form Tạo theo thẻ tab, chọn loại giấy tờ (CCCD 12 số / CMND 10 số), ràng số cho CCCD/điện thoại/MST, chọn khách theo tên.
- **Tạo dịch vụ:** chọn khách hàng theo tên (bỏ gõ ID).
- **Nhật ký kiểm toán:** hiển thị TÊN người thực hiện/đối tượng thay vì ID.
- (Kèm các nhóm cũ chưa từng lên `main`: thẻ mộ + watermark, đính kèm mộ, an toàn tài khoản…)

**Migration mới:** `V0045__care_package_pricing_basis.sql` — chỉ THÊM cột `Service_Types.pricing_basis` (mặc định `PER_COT`), an toàn dữ liệu cũ. Rollback: `U0045`.

## 2. Kiểm tra trước (đã xanh)
- Backend build (Release) + unit test: OK
- Frontend lint + build + test: **555/555**
- CI `main` (GitHub Actions): build → test → docker build

## 3. ⚠️ Lưu ý bắt buộc trước khi chạy
1. **DB production = `PTKD_PROD` trên container `db`** (cổng 1433, user `sa`) — KHÔNG phải `PTKD_DEV`. Migration V0045 áp thủ công trong SSMS lúc trước rất có thể đã nhắm `PTKD_DEV`, nên **vẫn phải chạy migrator** để `PTKD_PROD` có V0045. Migrator idempotent (ghi `SchemaVersions` + `IF NOT EXISTS`).
2. **Mật khẩu `sa`**: container db đang chạy với mật khẩu đặt từ trước (KHÁC mặc định trong compose). Phải tạo `.env` ở gốc repo với đúng mật khẩu, nếu không migrator/backend sẽ *Login failed*:
   ```
   DB_SA_PASSWORD=<mật khẩu sa thật của container>
   DB_NAME=PTKD_PROD
   ASPNETCORE_ENVIRONMENT=Production
   FRONTEND_PORT=80
   BACKEND_PORT=8080
   CORS_ORIGIN=http://localhost
   ```
3. **Thứ tự: migration TRƯỚC, app SAU.**

## 4. Các bước deploy (chạy tại gốc repo trên máy production)
```bash
# 4.1 Lấy code mới
git checkout main && git pull origin main   # kỳ vọng tip = cdbeb61

# 4.2 Áp migration cho PTKD_PROD (idempotent)
docker compose --profile migrate run --rm migrator

# 4.3 Build image app
docker compose build backend frontend

# 4.4 Khởi động
docker compose up -d db backend frontend
```

## 5. Kiểm tra sau deploy (dán kết quả cho AI soi)
```bash
docker compose ps          # db + backend + frontend đều Up/healthy
```
- Mở frontend (cổng 80) → đăng nhập → **Trang chủ** hiện dashboard, **Bán gói chăm sóc** / **Tạo dịch vụ** có ô chọn theo tên.
- Kiểm cột: `SELECT name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Service_Types') AND name='pricing_basis';` trên `PTKD_PROD` → phải có.

## 6. Rollback nếu cần
- **App:** `docker compose down` rồi deploy lại image bản trước.
- **DB (chỉ khi thật cần):** chạy `database/rollbacks/U0045__care_package_pricing_basis.sql` trên `PTKD_PROD` (gỡ cột `pricing_basis`). Lưu ý: gỡ cột sau khi app mới đã dùng sẽ làm app mới lỗi — chỉ rollback DB khi đã rollback app.
