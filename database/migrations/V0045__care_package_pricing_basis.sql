-- V0045__care_package_pricing_basis.sql
--
-- Cách tính giá gói chăm sóc THEO ĐỊNH NGHĨA dịch vụ (Service_Types):
--   PER_COT   = tính theo số cốt   (thành tiền = đơn giá × số cốt)
--   PER_GRAVE = tính theo phần mộ  (thành tiền = đơn giá, KHÔNG nhân cốt)
-- Mặc định PER_COT để KHỚP đúng hành vi cũ (LineSubtotal = đơn giá × cốt) — hồ sơ cũ không đổi.
--
-- Chỉ thêm CỘT MỚI, không đổi/không xoá gì → an toàn với dữ liệu đã có trên production.

SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Service_Types') AND name = 'pricing_basis')
    ALTER TABLE dbo.Service_Types
        ADD pricing_basis nvarchar(20) NOT NULL
            CONSTRAINT DF_Service_Types_pricing_basis DEFAULT ('PER_COT');
GO
