-- V0043__cemetery_card_watermark.sql
--
-- Hoa văn chìm (watermark) của thẻ mộ — cấu hình THEO TỪNG NGHĨA TRANG.
-- Thêm cột Cemeteries.card_watermark_code: mã mẫu hoa văn áp cho thẻ của nghĩa trang đó.
--   NULL / rỗng = không hoa văn. Giai đoạn 1 là các mã dựng sẵn (LOTUS, FRAME_CLASSIC,
--   DIAGONAL_TEXT). Giai đoạn 2 (upload mẫu) sẽ dùng mã dạng UPLOAD:{id} — nên để nvarchar rộng.

SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Cemeteries') AND name = 'card_watermark_code')
    ALTER TABLE dbo.Cemeteries ADD card_watermark_code nvarchar(64) NULL;
GO
