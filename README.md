# PTKD ERP

Hệ thống quản lý PTKD - INDEVCO ERP.

## Yêu cầu phần mềm
- **Backend:** .NET 10 SDK
- **Frontend:** Node.js (v18 trở lên), npm (hoặc yarn/pnpm)
- **Database:** Microsoft SQL Server (LocalDB hoặc SQL Server instance)

## Cách cấu hình SQL Server local
1. Mở SQL Server Management Studio (SSMS) hoặc Azure Data Studio.
2. Tạo database: `CREATE DATABASE PTKD_DEV;` (Nếu chưa có).
3. (Tùy chọn) Chạy `PTKD.DbMigrator` để khởi tạo bảng quản lý migration.

## Cách đặt .NET User Secrets
Dùng .NET User Secrets để cấu hình chuỗi kết nối an toàn ở local:
```bash
cd src/backend/PTKD.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=PTKD_DEV;Trusted_Connection=True;TrustServerCertificate=True"
```
*(Nếu bạn dùng DbMigrator, hãy cấu hình secret cho cả thư mục `src/backend/PTKD.DbMigrator`)*

## Cách chạy backend
```bash
# Hoặc sử dụng script: .\scripts\run-backend.ps1
cd src/backend/PTKD.Api
dotnet run
```
API sẽ chạy tại: `http://localhost:5057`
Mở trình duyệt truy cập: `http://localhost:5057/swagger`

## Cách chạy frontend
```bash
# Hoặc sử dụng script: .\scripts\run-frontend.ps1
cd src/frontend
npm install
npm run dev
```
Giao diện chạy tại: `http://localhost:5173`

## Cách build
```bash
# Sử dụng script tổng: .\scripts\build-all.ps1
# Build Backend:
dotnet build PTKD-ERP.sln

# Build Frontend:
cd src/frontend
npm run build
```

## Cách test
```bash
# Sử dụng script tổng: .\scripts\test-all.ps1
# Test Backend:
dotnet test PTKD-ERP.sln

# Test Frontend:
cd src/frontend
npm run test
```

## Cách chạy DbMigrator (Dry-Run)
DbMigrator hỗ trợ chế độ dry-run (chạy thử nhưng không commit transaction vào database):
```bash
cd src/backend/PTKD.DbMigrator
dotnet run -- --dry-run
```

## Deploy bằng Docker

### Yêu cầu
- Docker Engine 24+ và Docker Compose v2

### Khởi động nhanh (development)
```bash
cp .env.example .env
# Sửa .env: đặt DB_SA_PASSWORD an toàn
docker compose up -d
```
Truy cập: `http://localhost` (frontend) · `http://localhost:8080/api/v2/health` (backend)

### Chạy migration
```bash
docker compose --profile migrate run --rm migrator
```
Dry-run:
```bash
docker compose --profile migrate run --rm migrator -- --dry-run
```

### Production (HTTPS)
1. Đặt cert vào `certs/fullchain.pem` và `certs/privkey.pem`
2. Sửa `.env`:
   ```
   DB_SA_PASSWORD=<mật-khẩu-mạnh>
   CORS_ORIGIN=https://ptkd.your-domain.com
   ```
3. Khởi động:
   ```bash
   docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```
4. Chạy migration:
   ```bash
   docker compose --profile migrate run --rm migrator
   ```

### Biến môi trường
| Biến | Mặc định | Mô tả |
|---|---|---|
| `DB_SA_PASSWORD` | `YourStr0ngP@ssword!` | Mật khẩu SA cho SQL Server |
| `DB_NAME` | `PTKD_PROD` | Tên database |
| `CORS_ORIGIN` | `http://localhost` | Origin cho CORS |
| `BACKEND_PORT` | `8080` | Port backend expose |
| `FRONTEND_PORT` | `80` | Port frontend expose |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Môi trường .NET |
