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

- [ ] Dọn untracked files ở root (script Python/PS1 phân tích — xác nhận với owner trước khi xóa)
- [ ] Rebase hoặc merge `feature/phase-1-organization` → `main`
- [ ] Xác nhận `main` build + test xanh
- [ ] Tag version (ví dụ `v1.0.0-rc1`)

**Trạng thái:** CHƯA BẮT ĐẦU
**Kết quả:**
_(sẽ điền sau khi thực hiện)_

---

## Giai đoạn 2: CI/CD pipeline

**Mục tiêu:** Tự động hóa build, test, publish artifact.

- [ ] Tạo GitHub Actions workflow: build → test → publish
- [ ] Tách profile `Development` / `Staging` / `Production`
- [ ] Cấu hình secrets management (connection string, JWT signing key) qua environment variables
- [ ] Viết Dockerfile cho backend (ASP.NET Core)
- [ ] Viết Dockerfile cho frontend (Nginx serve static)
- [ ] Viết `docker-compose.yml` (backend + frontend + SQL Server dev)
- [ ] Xác nhận CI chạy xanh trên GitHub

**Trạng thái:** CHƯA BẮT ĐẦU
**Kết quả:**
_(sẽ điền sau khi thực hiện)_

---

## Giai đoạn 3: Chuẩn bị & deploy production

**Mục tiêu:** Cấu hình production, deploy staging, rồi production.

- [ ] `appsettings.Production.json` — chỉ giữ cấu trúc, secret từ env
- [ ] Health check endpoint
- [ ] HTTPS / reverse proxy config
- [ ] CORS config cho production domain
- [ ] Migration strategy cho DB production (backup → dry-run → apply)
- [ ] Deploy staging + smoke test
- [ ] Người có thẩm quyền phê duyệt
- [ ] Deploy production

**Trạng thái:** CHƯA BẮT ĐẦU
**Kết quả:**
_(sẽ điền sau khi thực hiện)_

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
