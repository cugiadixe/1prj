# HTTPS cho PTKD-ERP bằng CA nội bộ (LAN)

App production truy cập qua LAN tại **https://10.45.3.207**. Vì không có tên miền công khai
nên dùng **CA nội bộ tự tạo** (không mua chứng chỉ). Mô hình:

- **CA nội bộ (root)** — `secrets/ca-cert.pem` (+ khoá `secrets/ca-key.pem`), hạn **10 năm**.
  File để phát cho client: **`secrets/ptkd-internal-ca.crt`**.
- **Cert server (leaf)** — `certs/fullchain.pem` + `certs/privkey.pem`, hạn **~1 năm**, do CA ký,
  SAN gồm `10.45.3.207`, `localhost`, `127.0.0.1`, hostname máy.

> ⚠️ Các file khoá/cert nằm trong `secrets/` và `certs/` đã được **.gitignore — KHÔNG commit**.
> Chúng chỉ tồn tại trên máy production. **Nên sao lưu `secrets/` vào nơi lưu bí mật an toàn**
> (mất CA thì phải tạo CA mới và cài lại lên mọi máy client).

---

## 1. Tạo / gia hạn chứng chỉ

Chạy tại gốc repo (cần `openssl` — có sẵn trong Git Bash):

```bash
bash scripts/gen-prod-cert.sh
```

- Lần đầu: tạo **CA** (10 năm) + **cert server** (1 năm).
- Các lần sau (gia hạn hằng năm): script **dùng lại CA cũ**, chỉ **ký lại cert server** mới.
  → **Client KHÔNG phải cài lại gì** vì CA vẫn còn hạn.

Sau khi chạy, nạp cert mới vào nginx:

```powershell
docker compose -f docker-compose.yml -f docker-compose.prod.yml restart frontend
```

Kiểm tra chuỗi hợp lệ:

```bash
openssl verify -CAfile secrets/ca-cert.pem secrets/server-cert.pem   # kỳ vọng: OK
```

Đổi IP/hostname/thêm tên miền: sửa biến `LAN_IP` / `HOSTNAME_LOCAL` ở đầu `scripts/gen-prod-cert.sh`
rồi chạy lại (cert server sẽ mang SAN mới; CA giữ nguyên).

---

## 2. Cài CA lên MÁY CLIENT (làm MỘT LẦN cho mỗi máy)

Chép `secrets/ptkd-internal-ca.crt` sang máy nhân viên rồi cài vào **Trusted Root**:

**Windows (PowerShell, quyền Administrator):**
```powershell
Import-Certificate -FilePath "C:\duong-dan\ptkd-internal-ca.crt" -CertStoreLocation Cert:\LocalMachine\Root
```
Hoặc: nhấp đúp file `.crt` → *Install Certificate* → *Local Machine* → *Place all certificates in the
following store* → **Trusted Root Certification Authorities**.

Sau khi cài, mở **https://10.45.3.207** trên máy đó sẽ **không còn cảnh báo "Not secure"**.

> Chrome/Edge dùng kho tin cậy Windows → cài như trên là đủ. **Firefox** có kho riêng: vào
> *Settings → Privacy & Security → Certificates → View Certificates → Authorities → Import* rồi chọn
> file `.crt`, tick "Trust this CA to identify websites".

---

## 3. Lịch gia hạn

- **Cert server**: hạn ~1 năm. Đặt nhắc lịch (VD mỗi tháng 8 hằng năm) chạy lại mục 1.
  Xem hạn hiện tại:
  ```bash
  openssl x509 -in certs/fullchain.pem -noout -enddate
  ```
- **CA**: hạn 10 năm. Khi gần hết mới cần tạo CA mới + cài lại lên client.

---

## 4. Rollback về HTTP (nếu cần gỡ SSL tạm)

```powershell
docker compose up -d frontend    # chạy base compose (không overlay) → chỉ HTTP:80
```
