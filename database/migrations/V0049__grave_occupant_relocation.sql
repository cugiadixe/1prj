-- V0049__grave_occupant_relocation.sql
--
-- BỐC/CẢI TÁNG: một người chỉ ở MỘT mộ tại một thời điểm; sau khi bốc thì suất chuyển RELOCATED,
-- giải phóng người (được đặt sang mộ khác) và chỗ trong mộ.
--
-- Thêm cột trạng thái suất cho Grave_Occupants. Dữ liệu cũ mặc định ACTIVE (đang an táng).
-- An toàn: chỉ thêm cột, không đụng dữ liệu hiện có.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Grave_Occupants') AND name = 'status')
    ALTER TABLE dbo.Grave_Occupants
        ADD status varchar(20) NOT NULL
            CONSTRAINT DF_Grave_Occupants_status DEFAULT ('ACTIVE');
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Grave_Occupants') AND name = 'relocated_at')
    ALTER TABLE dbo.Grave_Occupants ADD relocated_at datetime2(3) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Grave_Occupants') AND name = 'relocation_note')
    ALTER TABLE dbo.Grave_Occupants ADD relocation_note nvarchar(500) NULL;
GO

-- Lọc nhanh suất đang hiệu lực theo mộ (đếm sức chứa) và theo người (chưa nằm mộ nào).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Grave_Occupants_status' AND object_id = OBJECT_ID('dbo.Grave_Occupants'))
    CREATE NONCLUSTERED INDEX IX_Grave_Occupants_status
        ON dbo.Grave_Occupants (status) INCLUDE (grave_id, deceased_customer_id);
GO
