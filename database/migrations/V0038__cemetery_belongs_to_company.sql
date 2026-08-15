-- V0038__cemetery_belongs_to_company.sql
--
-- NGHĨA TRANG THUỘC MỘT CÔNG TY; MỘ THUỘC CÔNG TY QUA NGHĨA TRANG.
-- (Anh Bách chốt 2026-08-15: mộ thuộc công ty qua nghĩa trang; nghĩa trang hiện có thuộc công ty
--  Hà Nội; nhân viên tập đoàn xem chéo qua cây "mẹ phủ con" — phần cây làm ở tầng mã nguồn.)
--
-- VÌ SAO. Trước đây dbo.Graves chỉ có cột `zone` (khu A–L) dạng chuỗi, KHÔNG có thực thể nào gắn
-- công ty. Nên không có cách nào lọc mộ theo công ty — 16 action của GravesController đều toàn cục,
-- gồm cả chuyển quyền sở hữu (hệ quả pháp lý thật). Migration này dựng thực thể còn thiếu.
--
-- PHẠM VI MIGRATION NÀY (nền, KHÔNG đổi hành vi kiểm quyền):
--   1. Bảng dbo.Cemeteries (mỗi nghĩa trang thuộc đúng một công ty).
--   2. Một nghĩa trang thuộc công ty Hà Nội.
--   3. Cột Graves.cemetery_id, gán toàn bộ mộ hiện có vào nghĩa trang đó, rồi chốt NOT NULL + FK.
-- Việc LỌC 16 action theo cemetery->company làm ở bước mã nguồn sau (Pha 2b).
--
-- CHỐT AN TOÀN: công ty Hà Nội resolve theo mã kết thúc '-HN' (khớp cả dev 'PTKD-HN' lẫn prod
-- 'INDEVCO-HN'). Nếu không đúng MỘT công ty -> THROW, dừng, để không gán nghĩa trang nhầm công ty.
-- Copy-based: không xoá/sửa dữ liệu mộ cũ, chỉ THÊM cột và gán.

SET XACT_ABORT ON;
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 1. Bảng Cemeteries — nghĩa trang
-- ══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Cemeteries' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Cemeteries
    (
        id                  bigint          IDENTITY(1,1)   NOT NULL,
        cemetery_code       nvarchar(50)                    NOT NULL,   -- mã nghĩa trang
        company_id          bigint                          NOT NULL,   -- nghĩa trang THUỘC đúng một công ty
        name                nvarchar(200)                   NOT NULL,
        address             nvarchar(500)                   NULL,
        is_active           bit             NOT NULL        CONSTRAINT DF_Cemeteries_is_active DEFAULT (1),
        created_at          datetime2(3)    NOT NULL        CONSTRAINT DF_Cemeteries_created_at DEFAULT (SYSUTCDATETIME()),
        created_by_user_id  bigint                          NULL,
        updated_at          datetime2(3)                    NULL,
        updated_by_user_id  bigint                          NULL,
        row_version         rowversion                      NOT NULL,

        CONSTRAINT PK_Cemeteries PRIMARY KEY (id),
        CONSTRAINT UQ_Cemeteries_code UNIQUE (cemetery_code),
        CONSTRAINT FK_Cemeteries_company_id FOREIGN KEY (company_id) REFERENCES dbo.Companies (id),
        CONSTRAINT FK_Cemeteries_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
        CONSTRAINT FK_Cemeteries_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id)
    );

    CREATE NONCLUSTERED INDEX IX_Cemeteries_company_id ON dbo.Cemeteries (company_id);
END
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 2. Một nghĩa trang thuộc công ty Hà Nội (idempotent)
-- ══════════════════════════════════════════════════════════════════════════
DECLARE @hnCompanyId bigint = (SELECT id FROM dbo.Companies WHERE company_code LIKE '%-HN');

IF @hnCompanyId IS NULL
    THROW 50038, N'V0038: không tìm thấy công ty Hà Nội (company_code kết thúc -HN). Dừng để không gán nghĩa trang nhầm công ty.', 1;

IF (SELECT COUNT(*) FROM dbo.Companies WHERE company_code LIKE '%-HN') > 1
    THROW 50038, N'V0038: có nhiều hơn một công ty có mã -HN. Cần xác định rõ công ty Hà Nội trước khi chạy.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Cemeteries WHERE cemetery_code = N'NT-HN-01')
    INSERT INTO dbo.Cemeteries (cemetery_code, company_id, name, address)
    VALUES (N'NT-HN-01', @hnCompanyId, N'Nghĩa trang Hà Nội', NULL);
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 3. Cột Graves.cemetery_id — thêm nullable trước để backfill
-- ══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Graves') AND name = 'cemetery_id')
    ALTER TABLE dbo.Graves ADD cemetery_id bigint NULL;
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 4. Gán toàn bộ mộ hiện có vào nghĩa trang Hà Nội (12 khu A–L cùng một nghĩa trang)
-- ══════════════════════════════════════════════════════════════════════════
UPDATE dbo.Graves
   SET cemetery_id = (SELECT id FROM dbo.Cemeteries WHERE cemetery_code = N'NT-HN-01')
 WHERE cemetery_id IS NULL;
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 5. Chốt NOT NULL + FK + index (chỉ khi không còn dòng NULL)
-- ══════════════════════════════════════════════════════════════════════════
IF EXISTS (SELECT 1 FROM dbo.Graves WHERE cemetery_id IS NULL)
    THROW 50038, N'V0038: còn mộ chưa gán nghĩa trang sau backfill. Dừng để không tạo cột NOT NULL trên dữ liệu thiếu.', 1;

ALTER TABLE dbo.Graves ALTER COLUMN cemetery_id bigint NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Graves_cemetery_id')
    ALTER TABLE dbo.Graves ADD CONSTRAINT FK_Graves_cemetery_id FOREIGN KEY (cemetery_id) REFERENCES dbo.Cemeteries (id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Graves_cemetery_id' AND object_id = OBJECT_ID('dbo.Graves'))
    CREATE NONCLUSTERED INDEX IX_Graves_cemetery_id ON dbo.Graves (cemetery_id);
GO
