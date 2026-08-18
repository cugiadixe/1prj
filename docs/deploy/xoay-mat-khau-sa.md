# Runbook: Xoay mật khẩu SA của SQL Server (production Docker)

> **Bối cảnh bảo mật:** Phát hiện #2 trong đánh giá bảo mật 2026-08-18. Trước đây không có
> file `.env`, nên mật khẩu `sa` được khởi tạo trong volume `sqldata` bằng đúng **mật khẩu mặc
> định công khai** `YourStr0ngP@ssword!` (nằm trong `docker-compose.yml` được git track).
> Runbook này xoay mật khẩu `sa` sang một chuỗi mạnh **riêng**, không commit.
>
> **Người thực hiện:** Đào Hải Bách (thao tác credential + production — không giao cho AI).
> **Thời điểm nên làm:** cửa sổ bảo trì, vì stack sẽ khởi động lại.

---

## 0. Mở terminal nào & lưu ý an toàn

**Chạy ở đâu:** terminal trên **chính máy production** (máy đang chạy Docker) — không phải terminal
bên trong container, không phải máy khác.

**Dùng terminal gì:** nên dùng **Git Bash** (đi kèm Git for Windows), vì các lệnh dưới viết theo cú
pháp bash (`openssl`, `printf`, `tr`, `cut`, nháy đơn `'...'`). PowerShell có cú pháp khác, dễ sai.

**Cách mở đúng thư mục:**
1. Mở File Explorer vào `C:\Projects\PTKD-ERP`.
2. Chuột phải khoảng trống → **"Git Bash Here"**.
3. (Nếu không có menu đó) mở Git Bash từ Start Menu rồi: `cd /c/Projects/PTKD-ERP`.
4. Kiểm tra đúng máy/đúng terminal — phải thấy dòng `ptkd-erp-db-1`:

```bash
docker ps -a
```

**Lưu ý an toàn:**

- Toàn bộ lệnh chạy tại thư mục gốc repo `C:\Projects\PTKD-ERP` (nơi có `docker-compose.yml`).
- Container DB tên **`ptkd-erp-db-1`** (kiểm bằng `docker ps -a`).
- ⚠️ **TUYỆT ĐỐI KHÔNG** dùng `docker compose down -v` — cờ `-v` xoá volume `sqldata` = **mất
  toàn bộ dữ liệu** production.
- ⚠️ Vì volume đã khởi tạo, SQL Server **KHÔNG** đổi mật khẩu `sa` theo biến môi trường khi
  khởi động lại. Bắt buộc đổi **bên trong DB** bằng `ALTER LOGIN` (Bước 3) khi DB còn chạy
  bằng mật khẩu cũ. Nếu bỏ qua bước này mà đặt thẳng mật khẩu mới vào `.env` rồi `up`, healthcheck
  và backend sẽ dùng mật khẩu mới trong khi DB vẫn giữ mật khẩu cũ → **kẹt, không kết nối được**.
- Đặt mật khẩu mới **tránh các ký tự** `;` `'` `"` `$` `\` và khoảng trắng — chúng phá chuỗi
  kết nối hoặc shell. Yêu cầu ≥ 12 ký tự, đủ hoa/thường/số/ký tự đặc biệt (chính sách SQL Server).
- ⚠️ **Git Bash trên Windows tự "dịch" đường dẫn** kiểu `/opt/...` thành `C:/Program Files/Git/opt/...`,
  làm `docker exec ... /opt/mssql-tools18/bin/sqlcmd` báo lỗi *"no such file or directory"* và
  **âm thầm không đổi được mật khẩu**. Vì vậy MỌI lệnh `docker exec ... sqlcmd` dưới đây đều thêm
  tiền tố `MSYS_NO_PATHCONV=1` để tắt cơ chế dịch này. (PowerShell không có vấn đề này.)

---

## 1. Sinh mật khẩu mạnh

Chạy lệnh sau, **lưu lại** kết quả (đây là mật khẩu production — không chia sẻ, không dán vào chat):

```bash
openssl rand -base64 24 | tr -d '/+=' | cut -c1-20
```

> Ở các bước dưới, thay mọi chỗ `MAT_KHAU_MOI` bằng đúng chuỗi vừa sinh.

---

## 2. Bật riêng DB bằng mật khẩu CŨ (mặc định)

Để container DB lên trạng thái healthy bằng mật khẩu hiện có trong volume:

```bash
DB_SA_PASSWORD='YourStr0ngP@ssword!' docker compose up -d db
```

Chờ ~15–30 giây cho DB sẵn sàng, kiểm tra bằng lệnh `docker` thường (cột STATUS phải `healthy`):

```bash
docker ps
```

> ⚠️ **Lưu ý giai đoạn chuyển tiếp:** vì `docker-compose.yml` nay bắt buộc `DB_SA_PASSWORD`,
> **mọi lệnh `docker compose ...` sẽ báo "required variable DB_SA_PASSWORD is missing"** cho tới
> khi file `.env` được tạo ở Bước 4. Đây đúng như thiết kế. Trước Bước 4, dùng lệnh `docker`
> thường (`docker ps`, `docker exec`, `docker logs`) thay cho `docker compose`.

---

## 3. Đổi mật khẩu SA bên trong DB

```bash
MSYS_NO_PATHCONV=1 docker exec ptkd-erp-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStr0ngP@ssword!' -C -Q "ALTER LOGIN sa WITH PASSWORD = 'MAT_KHAU_MOI';"
```

Lệnh chạy không báo lỗi = đã đổi. Nếu thấy lỗi `"...Git/opt/mssql-tools18...: no such file"` nghĩa
là thiếu `MSYS_NO_PATHCONV=1` (Git Bash dịch đường dẫn) — thêm vào rồi chạy lại. Nếu báo mật khẩu
không đạt chính sách, chọn mật khẩu mạnh hơn ở Bước 1 rồi lặp lại.

---

## 4. Tạo file `.env`

File `.env` đã được `.gitignore` bỏ qua (vá #4) nên an toàn khỏi commit nhầm. Tạo mới:

```bash
printf 'DB_SA_PASSWORD=MAT_KHAU_MOI\nDB_NAME=PTKD_PROD\nASPNETCORE_ENVIRONMENT=Production\nCORS_ORIGIN=https://ptkd.example.com\n' > .env
```

> Chỉnh `CORS_ORIGIN` cho đúng domain thật nếu khác.

---

## 5. Dựng lại toàn stack

```bash
docker compose up -d
```

Compose đọc `.env`; DB khởi động lại với mật khẩu mới (đã đổi ở Bước 3, được lưu bền trong volume),
healthcheck dùng mật khẩu mới sẽ PASS, backend kết nối bằng mật khẩu mới.

---

## 6. Kiểm chứng

```bash
docker compose ps
```

Tất cả service phải `running`/`healthy`. Xác nhận đăng nhập DB bằng mật khẩu mới:

```bash
MSYS_NO_PATHCONV=1 docker exec ptkd-erp-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'MAT_KHAU_MOI' -C -Q "SELECT 1"
```

Trả về `1` = thành công. Kiểm tra backend không còn lỗi đăng nhập DB:

```bash
docker compose logs --tail=50 backend
```

---

## Khắc phục sự cố

| Triệu chứng | Nguyên nhân | Xử lý |
|---|---|---|
| Backend log "Login failed for user 'sa'" | Mật khẩu trong `.env` ≠ mật khẩu đã đặt ở Bước 3 | Sửa `.env` cho khớp, `docker compose up -d` |
| DB `unhealthy` sau Bước 5 | Healthcheck dùng mật khẩu mới nhưng Bước 3 chưa chạy/không khớp | Lặp lại Bước 2–3 với mật khẩu cũ, đặt lại đúng mật khẩu mới |
| Quên mật khẩu cũ | — | Mật khẩu mặc định là `YourStr0ngP@ssword!` (nếu chưa từng đổi) |
| `sqlcmd: no such file or directory` (đường dẫn có `Git/opt/...`) | Git Bash dịch đường dẫn `/opt/...` | Thêm `MSYS_NO_PATHCONV=1` trước `docker exec` |
| Backend đứng ở `Created`, DB `unhealthy` | Bước 3 (ALTER LOGIN) chưa chạy thật (thường do lỗi đường dẫn trên) → SA vẫn mật khẩu cũ ≠ `.env` | Chạy lại Bước 3 với `MSYS_NO_PATHCONV=1` (xác thực bằng mật khẩu **cũ**, đặt sang giá trị trong `.env`), rồi `docker compose up -d` |

---

## Sau khi xong — nâng cấp least-privilege (khuyến nghị, làm sau)

App hiện kết nối bằng chính `sa` (toàn quyền sysadmin) — vi phạm nguyên tắc tối thiểu quyền. Bước
kế tiếp nên tạo **login riêng ít quyền** cho app (chỉ `db_datareader` + `db_datawriter` + `EXECUTE`
trên `PTKD_PROD`), đổi chuỗi kết nối sang login đó, dành `sa` chỉ cho quản trị. Việc này tách thành
runbook riêng khi sẵn sàng.

---

## Nhật ký thực hiện

| Ngày | Người làm | Ghi chú |
|---|---|---|
| | | |
