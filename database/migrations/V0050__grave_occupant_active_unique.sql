-- V0050__grave_occupant_active_unique.sql
--
-- SỬA LỖI chặn TÁI AN TÁNG sau cải táng.
-- Index cũ UQ_Grave_Occupants_deceased_customer_id (V0021) chỉ lọc `deceased_customer_id IS NOT NULL`,
-- KHÔNG lọc trạng thái → mỗi khách chỉ được đúng 1 dòng occupant ở MỌI trạng thái. Nhưng V0049 cho
-- phép bốc/cải táng (suất RELOCATED) rồi chôn lại, còn AddOccupantAsync tạo DÒNG MỚI khi đặt lại →
-- INSERT dòng thứ 2 cho cùng khách vi phạm index cũ ⇒ không thể tái an táng.
--
-- Bất biến ĐÚNG (theo quyết định "một người một suất tại một thời điểm"): mỗi khách chỉ 1 suất ĐANG
-- HIỆU LỰC (status='ACTIVE'); các suất đã bốc (RELOCATED) giữ lại làm lịch sử. Vì index cũ nghiêm
-- hơn (cấm mọi trùng), dữ liệu hiện có chắc chắn không có 2 suất ACTIVE/khách ⇒ tạo index mới an toàn.
-- Chỉ đổi index, không đụng dữ liệu.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'UQ_Grave_Occupants_deceased_customer_id'
             AND object_id = OBJECT_ID('dbo.Grave_Occupants'))
    DROP INDEX UQ_Grave_Occupants_deceased_customer_id ON dbo.Grave_Occupants;
GO

-- Mỗi khách chỉ 1 suất ĐANG HIỆU LỰC (ACTIVE); suất đã bốc (RELOCATED) không tính, cho đặt lại.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UQ_Grave_Occupants_active_customer'
                 AND object_id = OBJECT_ID('dbo.Grave_Occupants'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Grave_Occupants_active_customer
        ON dbo.Grave_Occupants (deceased_customer_id)
        WHERE deceased_customer_id IS NOT NULL AND status = 'ACTIVE';
GO
