# Bảo mật — Việc còn treo (handoff cho phiên sau)

> Trạng thái tính đến **2026-08-18**. Đợt đánh giá bảo mật toàn dự án đã xử lý 9 phát hiện
> (#1–#5, #7, #8 đã vá; #2 mật khẩu SA đã xoay; #6, #9 chốt giữ nguyên — xem
> `docs/decisions/2026-08-18-security-review-owner-decisions.md`). Còn **2 mục tuỳ chọn** dưới đây,
> **không gấp**, làm khi có cửa sổ bảo trì. Cả hai đều là thao tác **production/credential** → do
> **Đào Hải Bách** thực hiện (AI soạn runbook + sửa code repo, không tự chạy `docker build/up`).
>
> Bối cảnh nền: production chạy Docker Compose trên máy dev, DB=`PTKD_PROD` (SQL Server 2025),
> container `ptkd-erp-db-1` / `ptkd-erp-backend-1`. **TUYỆT ĐỐI KHÔNG** `docker compose down -v`
> (xoá volume = mất dữ liệu). Trên Git Bash, mọi `docker exec ... /opt/.../sqlcmd` phải thêm tiền
> tố `MSYS_NO_PATHCONV=1` (Git Bash dịch đường dẫn `/opt/...`). Tham khảo mẫu:
> `docs/deploy/xoay-mat-khau-sa.md`.

---

## Mục A — Tạo login DB ít quyền thay cho `sa` (least-privilege)

### Vì sao
Hiện app (backend) kết nối DB bằng chính `sa` — tài khoản sysadmin toàn quyền. Nếu lộ chuỗi kết nối
hoặc có SQL injection (hiện chưa có), kẻ tấn công chiếm trọn máy chủ DB. Nguyên tắc tối thiểu quyền:
backend chỉ cần đọc/ghi dữ liệu + chạy stored procedure, **không** cần quyền quản trị.

### Hiện trạng
- Chuỗi kết nối backend nằm trong `docker-compose.yml` (service `backend`), **hard-code** `User Id=sa`,
  mật khẩu lấy từ biến `${DB_SA_PASSWORD}`.
- Service `migrator` cũng dùng `sa` — **giữ nguyên** vì migrator chạy DDL (đổi schema), cần quyền cao.
  Chỉ đổi **backend** sang login ít quyền.

### Các bước

**A1. Tạo login + user + cấp quyền** (chạy trên máy production, Git Bash). Thay `MAT_KHAU_APP` bằng
mật khẩu mạnh riêng (khác mật khẩu `sa`), và `MAT_KHAU_SA` bằng mật khẩu `sa` hiện tại:

```bash
MSYS_NO_PATHCONV=1 docker exec ptkd-erp-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'MAT_KHAU_SA' -C -Q "IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name='ptkd_app') CREATE LOGIN ptkd_app WITH PASSWORD='MAT_KHAU_APP', CHECK_POLICY=ON; USE PTKD_PROD; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name='ptkd_app') CREATE USER ptkd_app FOR LOGIN ptkd_app; ALTER ROLE db_datareader ADD MEMBER ptkd_app; ALTER ROLE db_datawriter ADD MEMBER ptkd_app; GRANT EXECUTE TO ptkd_app;"
```

> Cấp: `db_datareader` (SELECT mọi bảng/view) + `db_datawriter` (INSERT/UPDATE/DELETE) + `EXECUTE`
> (chạy stored procedure trong `database/procedures`). KHÔNG cấp `db_owner`/`db_ddladmin`.
> Audit append-only dùng INSTEAD OF trigger ở tầng DB nên vẫn chặn UPDATE/DELETE bảng audit — không
> ảnh hưởng.

**A2. Thêm mật khẩu app vào `.env`** (file đã .gitignore):

```bash
printf 'DB_APP_PASSWORD=MAT_KHAU_APP\n' >> .env
```

**A3. Sửa `docker-compose.yml`** — đổi chuỗi kết nối service `backend` sang `ptkd_app` (AI làm phần
code này). Đổi:

```
ConnectionStrings__DefaultConnection: "Server=db;Database=${DB_NAME:-PTKD_PROD};User Id=sa;Password=${DB_SA_PASSWORD:?...};TrustServerCertificate=True"
```

thành (service `backend` thôi, GIỮ NGUYÊN service `migrator` dùng sa):

```
ConnectionStrings__DefaultConnection: "Server=db;Database=${DB_NAME:-PTKD_PROD};User Id=ptkd_app;Password=${DB_APP_PASSWORD:?DB_APP_PASSWORD chua dat trong .env};TrustServerCertificate=True"
```

**A4. Dựng lại backend + kiểm chứng:**

```bash
docker compose up -d backend
```

```bash
curl http://localhost:8080/api/v2/health
```

Phải trả `"status":"Healthy"`. Sau đó **test kỹ**: đăng nhập, xem danh sách khách/mộ, tạo 1 bản ghi,
chạy 1 báo cáo/dashboard. Nếu có lỗi kiểu *"The SELECT permission was denied"* → thiếu quyền cho một
đối tượng cụ thể; cấp bổ sung rồi thử lại.

### Rollback
Đổi lại chuỗi kết nối backend về `User Id=sa;Password=${DB_SA_PASSWORD...}`, `docker compose up -d backend`.

---

## Mục B — Kích hoạt khoá ký JWT bền ở production

### Vì sao
`JwtSigningKeyProvider` (`src/backend/PTKD.Infrastructure/Security/Cryptography/JwtSigningKeyProvider.cs`)
đã hỗ trợ nạp khoá RSA bền từ cấu hình (làm ở đợt #7). **Nhưng production chưa cấu hình** → provider
vẫn sinh khoá TẠM trong bộ nhớ mỗi lần khởi động, kèm log cảnh báo. Hệ quả: **mỗi lần restart backend,
mọi access token đang lưu hành bị vô hiệu** (người dùng bị đăng xuất), và nếu chạy nhiều bản sao
backend thì chúng không xác thực token của nhau.

### Hiện trạng
- Code đọc theo thứ tự: `Jwt:SigningKeyPath` (đường dẫn file PEM) → `Jwt:SigningKeyPem` (nội dung PEM).
- Chưa có biến nào được đặt trong `docker-compose.yml`/`.env`.
- `kid` suy tất định từ SHA-256(public key) → cùng khoá luôn cùng kid, token cũ vẫn khớp qua restart.

### Các bước

**B1. Sinh khoá RSA 2048-bit** (giữ kín, KHÔNG commit):

```bash
openssl genrsa -out certs/jwt-signing.pem 2048
```

> Thư mục `certs/` đã được compose prod dùng cho cert TLS. Đảm bảo file khoá **không bị commit** —
> thêm dòng `certs/` (hoặc `*.pem`) vào `.gitignore` nếu chưa có.

**B2. Sửa `docker-compose.yml`** service `backend` (AI làm) — thêm biến môi trường + mount file:

```yaml
  backend:
    environment:
      Jwt__SigningKeyPath: /run/secrets/jwt-signing.pem
    volumes:
      - ./certs/jwt-signing.pem:/run/secrets/jwt-signing.pem:ro
```

**B3. Dựng lại backend:**

```bash
docker compose up -d backend
```

**B4. Kiểm chứng khoá đã bền:**

```bash
docker compose logs backend | grep -i "kh\|jwt\|kid" | tail
```

- Phải thấy log `Đã nạp khoá ký JWT bền từ cấu hình (kid=...)`, **KHÔNG** còn dòng cảnh báo
  *"đang dùng khoá TẠM"*.
- Test bền qua restart: đăng nhập lấy token → `docker compose restart backend` → gọi 1 endpoint cần
  xác thực bằng **đúng token cũ** → vẫn `200` (trước đây sẽ `401` vì khoá đổi).

### Rollback
Gỡ biến `Jwt__SigningKeyPath` + volume mount khỏi compose, `docker compose up -d backend` (quay lại
khoá tạm).

---

## Ghi chú cho AI phiên sau
- Đọc memory `project-security-review` và `project-production-docker-live` trước.
- Hai mục trên **độc lập nhau**, làm mục nào trước cũng được.
- Phần **sửa `docker-compose.yml`** (A3, B2) là code repo — AI làm được ngay, commit như thường.
  Phần **chạy lệnh production** (tạo login, sinh khoá, `docker compose up`) là của owner.
- Sau khi làm xong mục nào, cập nhật lại file này + memory + `DEPLOYMENT-PLAN.md`.
