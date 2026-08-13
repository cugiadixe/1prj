-- V0019__grave_occupant_owner_relationship.sql
-- Quan hệ gia đình 2 chiều giữa chủ mộ (khách hàng) và từng người an táng (cốt):
--   owner_relationship    : chủ mộ  → người mất  (vd 'Con trai')
--   deceased_relationship : người mất → chủ mộ   (vd 'Bố đẻ')

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Grave_Occupants') AND name = 'owner_relationship')
    ALTER TABLE dbo.Grave_Occupants ADD owner_relationship nvarchar(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Grave_Occupants') AND name = 'deceased_relationship')
    ALTER TABLE dbo.Grave_Occupants ADD deceased_relationship nvarchar(100) NULL;
GO
