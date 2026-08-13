-- V0020__customer_status_deceased.sql
-- Thêm trạng thái 'DECEASED' (đã mất) cho khách hàng.
--   - Cốt (người an táng) sẽ là một Customer có customer_status = 'DECEASED'.
--   - Chi tiết ngày/nơi mất tái dùng bảng dbo.Profiles (đã có sẵn death_date_*, death_place).
-- An toàn: chỉ mở rộng danh sách giá trị hợp lệ; không đụng dữ liệu hiện có.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

-- Bỏ CHECK cũ (ACTIVE/INACTIVE/MERGED) rồi thêm lại có DECEASED
IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_Customers_customer_status'
             AND parent_object_id = OBJECT_ID('dbo.Customers'))
    ALTER TABLE dbo.Customers DROP CONSTRAINT CK_Customers_customer_status;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = 'CK_Customers_customer_status'
                 AND parent_object_id = OBJECT_ID('dbo.Customers'))
    ALTER TABLE dbo.Customers
        ADD CONSTRAINT CK_Customers_customer_status
        CHECK (customer_status IN ('ACTIVE', 'INACTIVE', 'MERGED', 'DECEASED'));
GO
