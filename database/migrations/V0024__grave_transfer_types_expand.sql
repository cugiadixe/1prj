-- V0024__grave_transfer_types_expand.sql
-- Mở rộng loại chuyển quyền sở hữu mộ: chuyển quyền là việc CHUNG (người còn sống),
-- không chỉ do qua đời. Thêm:
--   RELOCATION = chuyển công tác / chuyển nơi ở        GIFT = cho / tặng
-- Giữ: SALE (sang nhượng) · INHERITANCE (thừa kế) · DEATH (chủ qua đời) · CORRECTION (đính chính)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_GOH_type' AND parent_object_id = OBJECT_ID('dbo.Grave_Ownership_History'))
    ALTER TABLE dbo.Grave_Ownership_History DROP CONSTRAINT CK_GOH_type;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = 'CK_GOH_type' AND parent_object_id = OBJECT_ID('dbo.Grave_Ownership_History'))
    ALTER TABLE dbo.Grave_Ownership_History
        ADD CONSTRAINT CK_GOH_type
        CHECK (transfer_type IN ('SALE', 'GIFT', 'RELOCATION', 'INHERITANCE', 'DEATH', 'CORRECTION'));
GO
