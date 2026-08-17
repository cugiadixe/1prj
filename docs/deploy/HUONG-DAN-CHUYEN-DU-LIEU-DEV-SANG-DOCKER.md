# Hướng dẫn chuyển dữ liệu PTKD_DEV → PTKD_PROD (Docker)

Mục tiêu: đưa **toàn bộ** dữ liệu đang có ở `PTKD_DEV` (SQL Server cài trên Windows) vào
`PTKD_PROD` (SQL Server trong container Docker), để production dùng chung một nơi lưu.

> Cách làm: **backup** DB dev ra file `.bak` → **copy** vào container → **restore** thành `PTKD_PROD`.
> Đây là di trú dữ liệu production: **owner chạy, AI verify**. Làm lần lượt từng bước.

Ký hiệu:
- `<PW>` = mật khẩu `sa` của container db (giống `.env` DB_SA_PASSWORD). **Đây là chỗ điền — phải thay bằng mật khẩu thật, đừng gõ nguyên chữ `<PW>`.**
- Container db tên `ptkd-erp-db-1` (đổi nếu `docker compose ps` hiện tên khác).

> **Không nhớ mật khẩu `sa`?** Lấy đúng chuỗi đã đặt khi dựng container:
> ```powershell
> docker exec ptkd-erp-db-1 printenv MSSQL_SA_PASSWORD
> ```
> Chuỗi in ra chính là `<PW>` (và cũng là `DB_SA_PASSWORD` cho `.env`).

---

## ⚠️ Trước khi làm
- **RESTORE sẽ GHI ĐÈ toàn bộ `PTKD_PROD` hiện tại.** Nếu `PTKD_PROD` đang có dữ liệu thật cần giữ → sao lưu nó trước (Bước 0b). Nếu `PTKD_PROD` chỉ trống/dữ liệu cũ bỏ được → bỏ qua 0b.

- **Tắt app production để không ai ghi dữ liệu khi đang di trú.** Cách chạy:
  1. Mở **Docker Desktop**, đợi báo "Engine running".
  2. Mở **PowerShell** rồi vào thư mục dự án:
     ```powershell
     cd C:\Projects\PTKD-ERP
     ```
  3. Chạy:
     ```powershell
     docker compose stop backend frontend
     ```
  - **Kỳ vọng:** vài dòng `Stopping ... Stopped` rồi về dấu nhắc.
  - **Lưu ý:** container `backend`/`frontend` hiện đang KHÔNG chạy (chỉ mỗi `db` chạy), nên lệnh này có thể chỉ báo *already stopped* / *no containers* — **không sao**, mục đích chỉ để chắc chắn không có gì ghi vào DB.

- **Dữ liệu anh nhập gần đây nằm ở bản DEV** (dotnet cổng 5057 + web 5173, trỏ `PTKD_DEV`), không phải ở Docker. Trước khi backup, chỉ cần **ngừng nhập liệu trên bản dev** một lát là đủ — không cần tắt thêm gì, vì lệnh `BACKUP` của SQL Server chạy được cả khi DB đang mở (bản backup vẫn nhất quán tại thời điểm chạy). Mọi dữ liệu nhập SAU khi backup sẽ không có trong bản chuyển.

### (Tuỳ chọn) Bước 0b — sao lưu PTKD_PROD hiện tại cho chắc
```powershell
docker exec ptkd-erp-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<PW>" -C -Q "IF DB_ID('PTKD_PROD') IS NOT NULL BACKUP DATABASE PTKD_PROD TO DISK='/var/opt/mssql/ptkd_prod_backup.bak' WITH INIT"
```

---

## Bước 1 — Backup PTKD_DEV ra file .bak (SQL Server trên Windows)
```powershell
sqlcmd -S . -E -C -Q "BACKUP DATABASE PTKD_DEV TO DISK='C:\Projects\PTKD-ERP\ptkd_dev.bak' WITH INIT, FORMAT"
```
- **Kỳ vọng:** `BACKUP DATABASE successfully processed ...`. File `ptkd_dev.bak` xuất hiện ở `C:\Projects\PTKD-ERP`.

## Bước 2 — Copy file .bak vào trong container db
```powershell
docker cp C:\Projects\PTKD-ERP\ptkd_dev.bak ptkd-erp-db-1:/var/opt/mssql/ptkd_dev.bak
```

## Bước 3 — Lấy tên file logic trong bản backup
```powershell
docker exec ptkd-erp-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<PW>" -C -Q "RESTORE FILELISTONLY FROM DISK='/var/opt/mssql/ptkd_dev.bak'"
```
- Ghi lại cột **LogicalName**: thường có 2 dòng — 1 dòng dữ liệu (vd `PTKD_DEV`) và 1 dòng log (vd `PTKD_DEV_log`). Dùng ở Bước 4.

## Bước 4 — Restore thành PTKD_PROD (ghi đè)
Thay `<DATA_LOGICAL>` và `<LOG_LOGICAL>` bằng tên lấy ở Bước 3:
```powershell
docker exec ptkd-erp-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<PW>" -C -Q "RESTORE DATABASE PTKD_PROD FROM DISK='/var/opt/mssql/ptkd_dev.bak' WITH MOVE '<DATA_LOGICAL>' TO '/var/opt/mssql/data/PTKD_PROD.mdf', MOVE '<LOG_LOGICAL>' TO '/var/opt/mssql/data/PTKD_PROD_log.ldf', REPLACE, RECOVERY"
```
- **Kỳ vọng:** `RESTORE DATABASE successfully processed ...`.
- Nếu lỗi *"version ... cannot be restored"*: SQL Server dev mới hơn container (2022) → nhắn em, cần đổi cách.

## Bước 5 — Kiểm tra dữ liệu trong PTKD_PROD
```powershell
docker exec ptkd-erp-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<PW>" -C -d PTKD_PROD -Q "SELECT (SELECT COUNT(*) FROM dbo.Customers) AS Customers, (SELECT COUNT(*) FROM dbo.Graves) AS Graves; SELECT TOP 1 Version FROM dbo.SchemaVersions ORDER BY Version DESC"
```
- **Kỳ vọng:** số khách/mộ khớp bản dev, và Version mới nhất là `V0045`.
- Dán kết quả cho em soi.

## Bước 6 — Bật lại app (hoặc tiếp tục deploy)
```powershell
docker compose up -d db backend frontend
docker compose ps
```

---

## Dọn dẹp (sau khi chắc chắn OK)
```powershell
Remove-Item C:\Projects\PTKD-ERP\ptkd_dev.bak
docker exec ptkd-erp-db-1 rm -f /var/opt/mssql/ptkd_dev.bak
```

## Lưu ý về sau
- Dữ liệu giờ nằm ở volume Docker `sqldata` → **giữ nguyên** qua restart. **Đừng** chạy `docker compose down -v` (xoá volume = mất dữ liệu).
- Nên đặt lịch **backup định kỳ** `PTKD_PROD` (Bước 0b) vì đây thành nguồn dữ liệu chính thức.
