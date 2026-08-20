#!/usr/bin/env bash
# =============================================================================
# Sinh chứng chỉ HTTPS cho PTKD-ERP (CA nội bộ + cert server) — dùng cho LAN.
#
# Mô hình:
#   - CA nội bộ (root)  : secrets/ca-key.pem + secrets/ca-cert.pem  (hạn 10 năm)
#         → CÀI MỘT LẦN file secrets/ptkd-internal-ca.crt vào "Trusted Root" mỗi máy client.
#   - Cert server (leaf): certs/privkey.pem + certs/fullchain.pem   (hạn ~1 năm)
#         → nginx.ssl.conf đọc 2 file này. RENEW = chạy lại script (CA cũ ký lại leaf,
#           client KHÔNG phải cài lại gì vì CA vẫn còn hạn).
#
# CHẠY:  bash scripts/gen-prod-cert.sh
#   (cần openssl — có sẵn trong Git Bash. Chạy tại gốc repo.)
#
# Sau khi chạy, bật HTTPS:
#   docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
# =============================================================================
set -euo pipefail
cd "$(dirname "$0")/.."   # về gốc repo

# --- Cấu hình: sửa SAN ở đây khi đổi IP/hostname/thêm tên miền ---
LAN_IP="${LAN_IP:-10.45.3.207}"
HOSTNAME_LOCAL="${HOSTNAME_LOCAL:-IND-L-BACHDH}"
CA_DAYS="${CA_DAYS:-3650}"      # CA: 10 năm
LEAF_DAYS="${LEAF_DAYS:-397}"   # cert server: ~1 năm (dưới ngưỡng 398 ngày của trình duyệt)

mkdir -p secrets certs
SAN="subjectAltName=DNS:localhost,DNS:${HOSTNAME_LOCAL},IP:127.0.0.1,IP:${LAN_IP}"

# 1) CA nội bộ — chỉ tạo nếu CHƯA có (để renew leaf không đổi CA → khỏi cài lại client)
if [[ ! -f secrets/ca-key.pem || ! -f secrets/ca-cert.pem ]]; then
  echo "[CA] Tạo CA nội bộ mới (hạn ${CA_DAYS} ngày)..."
  openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:4096 -out secrets/ca-key.pem
  openssl req -x509 -new -key secrets/ca-key.pem -sha256 -days "${CA_DAYS}" \
    -out secrets/ca-cert.pem \
    -subj "/O=INDEVCO/CN=PTKD-ERP Internal CA" \
    -addext "basicConstraints=critical,CA:TRUE" \
    -addext "keyUsage=critical,keyCertSign,cRLSign"
  cp secrets/ca-cert.pem secrets/ptkd-internal-ca.crt   # file .crt để cài vào Trusted Root
  echo "[CA] Đã tạo. CÀI 'secrets/ptkd-internal-ca.crt' vào Trusted Root trên mỗi máy client (một lần)."
else
  echo "[CA] Dùng lại CA sẵn có (secrets/ca-cert.pem) — client không cần cài lại."
fi

# 2) Cert server (leaf) — luôn tạo mới khi chạy (đây là bước RENEW hằng năm)
echo "[LEAF] Sinh cert server (hạn ${LEAF_DAYS} ngày, SAN=${SAN#subjectAltName=})..."
EXT_FILE="secrets/leaf-ext.cnf"   # path tương đối để openssl (kể cả bản Windows) đọc được
printf '%s\nbasicConstraints=CA:FALSE\nkeyUsage=digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\n' "$SAN" > "$EXT_FILE"
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out certs/privkey.pem
openssl req -new -key certs/privkey.pem -out secrets/server.csr \
  -subj "/O=INDEVCO/CN=${LAN_IP}"
openssl x509 -req -in secrets/server.csr \
  -CA secrets/ca-cert.pem -CAkey secrets/ca-key.pem -CAcreateserial \
  -out secrets/server-cert.pem -days "${LEAF_DAYS}" -sha256 \
  -extfile "$EXT_FILE"

# 3) fullchain = leaf + CA (nginx cần chuỗi đầy đủ)
cat secrets/server-cert.pem secrets/ca-cert.pem > certs/fullchain.pem
rm -f secrets/server.csr "$EXT_FILE"

echo ""
echo "XONG. Đã ghi: certs/fullchain.pem + certs/privkey.pem"
echo "Hạn cert server đến: $(openssl x509 -in secrets/server-cert.pem -noout -enddate | cut -d= -f2)"
echo "Áp dụng: docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d frontend"
