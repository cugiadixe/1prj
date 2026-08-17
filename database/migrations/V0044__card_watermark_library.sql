-- V0044__card_watermark_library.sql
--
-- Thư viện mẫu hoa văn (watermark) TẢI LÊN — dùng chung trong một công ty. Nghĩa trang chọn mẫu
-- qua mã "UPLOAD:{id}" (lưu ở Cemeteries.card_watermark_code). Ảnh lưu thẳng trong DB (ít, nhỏ).

SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.Card_Watermarks'))
BEGIN
    CREATE TABLE dbo.Card_Watermarks
    (
        id                  bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Card_Watermarks PRIMARY KEY,
        company_id          bigint          NOT NULL,
        name                nvarchar(200)   NOT NULL,
        content_type        nvarchar(100)   NOT NULL,
        image_bytes         varbinary(max)  NOT NULL,
        is_active           bit             NOT NULL CONSTRAINT DF_Card_Watermarks_is_active DEFAULT (1),
        created_at          datetime2       NOT NULL,
        created_by_user_id  bigint          NULL,
        CONSTRAINT FK_Card_Watermarks_Company FOREIGN KEY (company_id) REFERENCES dbo.Companies(id)
    );

    CREATE INDEX IX_Card_Watermarks_company ON dbo.Card_Watermarks(company_id);
END
GO
