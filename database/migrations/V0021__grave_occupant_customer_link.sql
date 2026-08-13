-- V0021__grave_occupant_customer_link.sql
-- Cốt (người an táng) trở thành Khách hàng:
--   - Grave_Occupants.deceased_customer_id  → FK Customers(id) (nguồn sự thật)
--   - Unique lọc: 1 người chỉ nằm trong 1 mộ
--   - Giữ cột full_name/dob/death_* cũ làm SNAPSHOT hiển thị (khỏi join nặng khi liệt kê)
-- An toàn: cột NULL, không phá dữ liệu/backend hiện có.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.Grave_Occupants') AND name = 'deceased_customer_id')
    ALTER TABLE dbo.Grave_Occupants ADD deceased_customer_id bigint NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
               WHERE name = 'FK_Grave_Occupants_deceased_customer_id'
                 AND parent_object_id = OBJECT_ID('dbo.Grave_Occupants'))
    ALTER TABLE dbo.Grave_Occupants
        ADD CONSTRAINT FK_Grave_Occupants_deceased_customer_id
        FOREIGN KEY (deceased_customer_id) REFERENCES dbo.Customers (id);
GO

-- 1 người (customer) chỉ được là cốt trong đúng 1 mộ
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UQ_Grave_Occupants_deceased_customer_id'
                 AND object_id = OBJECT_ID('dbo.Grave_Occupants'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Grave_Occupants_deceased_customer_id
        ON dbo.Grave_Occupants (deceased_customer_id)
        WHERE deceased_customer_id IS NOT NULL;
GO
