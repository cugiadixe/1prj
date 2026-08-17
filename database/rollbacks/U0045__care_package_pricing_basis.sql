-- U0045__care_package_pricing_basis.sql — hoàn tác V0045.
-- Gỡ cột pricing_basis (và ràng buộc mặc định) khỏi Service_Types.

SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Service_Types') AND name = 'pricing_basis')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_Service_Types_pricing_basis')
        ALTER TABLE dbo.Service_Types DROP CONSTRAINT DF_Service_Types_pricing_basis;
    ALTER TABLE dbo.Service_Types DROP COLUMN pricing_basis;
END
GO
