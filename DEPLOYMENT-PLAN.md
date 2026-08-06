# PTKD-ERP — Kế hoạch triển khai Production

> **Quy tắc:** Mỗi phiên làm việc, AI PHẢI đọc file này trước tiên (sau AGENTS.md).
> Sau mỗi giai đoạn hoàn thành, cập nhật trạng thái và kết quả vào đây.
>
> Người phê duyệt: **Đào Hải Bách** · Ngày lập: 06/08/2026
> Nhánh làm việc: `feature/phase-1-organization`

---

## Quyết định đã chốt

| # | Quyết định | Giá trị |
|---|---|---|
| 1 | Môi trường production | Docker / Container |
| 2 | Xác thực | JWT nội bộ (giữ nguyên, AD/LDAP sau) |
| 3 | Bắt đầu từ | Giai đoạn 0 — xác minh baseline |

---

## Giai đoạn 0: Tiếp nhận & xác minh baseline

**Mục tiêu:** Build + chạy test toàn bộ, ghi nhận baseline pass/fail trước khi thay đổi bất kỳ gì.

- [x] Build backend (`dotnet build` toàn solution)
- [x] Build frontend (`npm run build`)
- [x] Chạy unit tests backend
- [x] Chạy integration tests backend (cần SQL Server)
- [x] Chạy API tests backend
- [x] Chạy frontend tests (`npm test`)
- [ ] Chạy 15 migration từ DB trống (dry-run nếu được) — HOÃN: cần DB trống riêng, rủi ro ảnh hưởng DB hiện tại. Sẽ làm trong Giai đoạn 2 với Docker compose.
- [x] Ghi nhận kết quả baseline bên dưới

**Trạng thái:** HOÀN THÀNH (06/08/2026)
**Kết quả:**

| Hạng mục | Kết quả | Chi tiết |
|---|---|---|
| Backend build | PASSED | 0 error, 9 warning (nullability + obsolete API trong test) |
| Frontend build | PASSED | 3275 modules, 1 chunk 1.5MB (warning code-split) |
| Unit tests | **236/236 PASSED** | 1 giây |
| Integration tests | **203/203 PASSED** | 2 phút 5 giây (SQL Server thật) |
| API tests | **308/308 PASSED** | 51 giây |
| Frontend tests | **500/500 PASSED** | 71 file, 125 giây |
| **Tổng** | **1247/1247 PASSED** | Không có test fail |

**Warning đáng chú ý (không chặn deploy):**
1. `SYSLIB0050` — `FormatterServices` obsolete trong `CustomerMasterChangeServiceTests.cs` (5 chỗ)
2. `CS8767`/`CS8625` — nullability mismatch trong `NoOpExecutionStrategy` (2 chỗ)
3. Frontend chunk > 500KB — nên code-split sau khi deploy thành công

---

## Giai đoạn 1: Merge về `main`

**Mục tiêu:** Đưa code về nhánh chính, dọn dẹp untracked files.

- [x] Dọn untracked files ở root — xóa 62 file script/output tạm, giữ 3 file tài liệu
- [x] Đổi tên nhánh `master` → `main` (theo yêu cầu owner)
- [x] Commit 3 file tài liệu: ARCHITECTURE.md, DEPLOYMENT-PLAN.md, DECISION-INDEX.md
- [x] Merge `feature/phase-1-organization` → `main` (no-ff)
- [x] Xác nhận `main` build (0 error) + unit test (236/236 passed)
- [x] Tag `v1.0.0-rc1`

**Trạng thái:** HOÀN THÀNH (06/08/2026)
**Kết quả:**
- Merge commit trên `main` thành công, không conflict
- Build: 0 error, 9 warning (giống baseline)
- Unit tests: 236/236 passed
- Tag: `v1.0.0-rc1`
- Chưa push remote (cần xác nhận owner trước khi push)

---

## Giai đoạn 2: CI/CD pipeline

**Mục tiêu:** Tự động hóa build, test, publish artifact.

- [x] Tạo GitHub Actions workflow: `.github/workflows/ci.yml` (build → test → docker build)
- [x] Tách profile: thêm `appsettings.Production.json`
- [x] Cấu hình secrets: connection string qua env var `ConnectionStrings__DefaultConnection`, `.env.example` mẫu
- [x] Viết Dockerfile backend: multi-stage (sdk build → aspnet runtime), port 8080
- [x] Viết Dockerfile frontend: multi-stage (node build → nginx serve), proxy `/api/` → backend
- [x] Viết `docker-compose.yml` (backend + frontend + SQL Server) + `docker-compose.dev.yml` (DB only)
- [x] Xác nhận Docker build + compose local — 3 container chạy thành công
- [x] Gỡ production guard Phase 1A.2 (Program.cs dòng 217-223, được owner phê duyệt)
- [x] Xác nhận CI chạy xanh trên GitHub — CI #1, commit `7b653df`, 5m51s, ALL GREEN

**Trạng thái:** HOÀN THÀNH (06/08/2026)
**Kết quả:**

| File | Mô tả |
|---|---|
| `.github/workflows/ci.yml` | CI: backend build+test, frontend lint+build+test, docker build |
| `src/backend/Dockerfile` | Multi-stage .NET 10 preview |
| `src/frontend/Dockerfile` | Multi-stage Node 22 + Nginx |
| `src/frontend/nginx.conf` | Reverse proxy `/api/` → backend, SPA fallback |
| `docker-compose.yml` | Production: backend + frontend + SQL Server |
| `docker-compose.dev.yml` | Dev: SQL Server only |
| `src/backend/PTKD.Api/appsettings.Production.json` | Production logging (Warning level) |
| `.env.example` | Mẫu biến môi trường |
| `src/backend/.dockerignore` | Loại bin/obj |
| `src/frontend/.dockerignore` | Loại node_modules/dist |

**Xác minh Docker local (06/08/2026):**
- Backend image: build OK, container chạy ổn sau khi gỡ production guard
- Frontend image: build OK, Nginx serve + proxy `/api/` → backend hoạt động
- SQL Server container: healthy
- Health endpoint (`/api/v2/health`): trả JSON, status Unhealthy (đúng — DB chưa có schema)
- Nginx proxy: request qua port 80 `/api/v2/health` → forward đúng sang backend port 8080

---

## Giai đoạn 3: Chuẩn bị & deploy production

**Mục tiêu:** Cấu hình production, deploy staging, rồi production.

- [x] `appsettings.Production.json` — CORS placeholder, Warning log level, secret từ env
- [x] Health check endpoint — đã có sẵn tại `/api/v2/health`
- [x] HTTPS / reverse proxy — `nginx.ssl.conf` + `docker-compose.prod.yml` (mount cert)
- [x] CORS config — chuyển từ hard-code sang `builder.Configuration`, override qua env `Cors__AllowedOrigins__0`
- [x] Migration strategy — Dockerfile.migrator + docker compose `--profile migrate`, hỗ trợ `--dry-run`
- [x] Hướng dẫn deploy — cập nhật README.md (Docker quick start + production HTTPS)
- [ ] Deploy staging + smoke test — CHỜ SERVER
- [ ] Người có thẩm quyền phê duyệt — CHỜ
- [ ] Deploy production — CHỜ SERVER

**Trạng thái:** CẤU HÌNH HOÀN THÀNH (06/08/2026) — chờ server để deploy

**Kết quả:**

| Hạng mục | Chi tiết |
|---|---|
| CORS | Linh hoạt qua env var, mặc định `localhost:5173` khi Development |
| HTTPS | `nginx.ssl.conf` redirect 80→443, mount cert vào `/etc/nginx/certs/` |
| Migration Docker | `docker compose --profile migrate run --rm migrator` (có dry-run) |
| Biến môi trường | `DB_SA_PASSWORD`, `DB_NAME`, `CORS_ORIGIN`, `BACKEND_PORT`, `FRONTEND_PORT` |
| Build + test | 0 error, 236/236 unit test passed sau thay đổi CORS |

**Khi có server, chạy:**
```
cp .env.example .env          # sửa mật khẩu + domain
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
docker compose --profile migrate run --rm migrator --dry-run
docker compose --profile migrate run --rm migrator
```

---

## Nhật ký thực hiện

| Ngày | Giai đoạn | Hành động | Kết quả |
|---|---|---|---|
| 06/08/2026 | GĐ 0 | Build backend + frontend | PASSED — 0 error, 9 warning |
| 06/08/2026 | GĐ 0 | Unit tests (236) | 236/236 PASSED |
| 06/08/2026 | GĐ 0 | Integration tests (203) | 203/203 PASSED |
| 06/08/2026 | GĐ 0 | API tests (308) | 308/308 PASSED |
| 06/08/2026 | GĐ 0 | Frontend tests (500) | 500/500 PASSED |
| 06/08/2026 | GĐ 0 | **Tổng kết GĐ 0** | **1247/1247 test PASSED. Baseline sạch.** |
| 06/08/2026 | GĐ 1 | Đổi master → main, dọn 62 file tạm | OK |
| 06/08/2026 | GĐ 1 | Commit 3 file tài liệu | `57400af` |
| 06/08/2026 | GĐ 1 | Merge feature → main (no-ff) | OK, 0 conflict |
| 06/08/2026 | GĐ 1 | Build + unit test trên main | 0 error, 236/236 passed |
| 06/08/2026 | GĐ 1 | Tag v1.0.0-rc1 | OK |
| 06/08/2026 | GĐ 1 | **Tổng kết GĐ 1** | **Main sạch, tagged, chưa push remote** |
| 06/08/2026 | GĐ 2 | Tạo Dockerfile backend + frontend | OK |
| 06/08/2026 | GĐ 2 | Tạo nginx.conf (reverse proxy) | OK |
| 06/08/2026 | GĐ 2 | Tạo docker-compose.yml + dev.yml | OK |
| 06/08/2026 | GĐ 2 | Tạo GitHub Actions CI workflow | OK |
| 06/08/2026 | GĐ 2 | Tạo appsettings.Production.json | OK |
| 06/08/2026 | GĐ 2 | Docker build local | SKIPPED — Docker daemon không chạy |
| 06/08/2026 | GĐ 2 | Gỡ production guard Phase 1A.2 | Owner phê duyệt, build OK |
| 06/08/2026 | GĐ 2 | Docker compose up (3 container) | Backend + Frontend + DB — tất cả chạy |
| 06/08/2026 | GĐ 2 | Health check + nginx proxy | OK — `/api/v2/health` qua cả port 8080 và 80 |
| 06/08/2026 | GĐ 2 | Commit CI/CD + Docker (13 file) | `7b653df` |
| 06/08/2026 | GĐ 2 | Push main + tag v1.0.0-rc1 | OK — remote `origin` |
| 06/08/2026 | GĐ 2 | CI #1 trên GitHub | ALL GREEN — 5m51s |
| 06/08/2026 | GĐ 2 | **Tổng kết GĐ 2** | **HOÀN THÀNH. Docker + CI + push đều xanh.** |
| 06/08/2026 | GĐ 3 | CORS linh hoạt qua env var | Build OK, 236/236 test passed |
| 06/08/2026 | GĐ 3 | nginx.ssl.conf (HTTPS) | Tạo mới |
| 06/08/2026 | GĐ 3 | docker-compose.prod.yml (HTTPS overlay) | Tạo mới |
| 06/08/2026 | GĐ 3 | Dockerfile.migrator + compose migrate profile | Tạo mới |
| 06/08/2026 | GĐ 3 | README.md — hướng dẫn Docker deploy | Cập nhật |
| 06/08/2026 | GĐ 3 | **Tổng kết GĐ 3** | **Cấu hình xong. Chờ server để deploy thật.** |
