-- V0042__attachment_storage_base_path.sql
--
-- "Nhớ gốc lưu của từng file đính kèm" — để đổi đường dẫn lưu trữ KHÔNG làm hỏng file cũ.
-- Thêm cột Grave_Attachments.storage_base_path: gốc lưu lúc file được ghi.
--   NULL = file cũ (ghi trước tính năng này) -> nằm ở GỐC MẶC ĐỊNH (appsettings). Không backfill
--   giá trị (tránh nhúng đường dẫn theo môi trường vào migration); tầng đọc tự lùi về mặc định.
--   File mới sẽ ghi rõ gốc hiện tại.

SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Grave_Attachments') AND name = 'storage_base_path')
    ALTER TABLE dbo.Grave_Attachments ADD storage_base_path nvarchar(1000) NULL;
GO
