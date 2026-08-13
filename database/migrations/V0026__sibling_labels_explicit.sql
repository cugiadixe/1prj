-- V0026__sibling_labels_explicit.sql
-- Nhãn anh/chị/em phải CỤ THỂ khi đã xác định được tuổi:
--   SIBLING_OLDER : "Anh" → "Anh trai", "Chị" → "Chị gái" (kèm giới tính rõ ràng).
--   SIBLING_YOUNGER: đã là "Em trai" / "Em gái" — giữ nguyên.
-- Nhãn gộp của SIBLING ("Anh/Em trai", "Chị/Em gái") chỉ còn dùng khi CHƯA đủ
-- ngày sinh để so tuổi; khi đó backend hiển thị nhãn trung tính "Anh/Chị/Em".
-- An toàn: chỉ cập nhật nhãn bảng tham chiếu, không đụng dữ liệu nghiệp vụ.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

UPDATE dbo.Relationship_Kinds
   SET label_male   = N'Anh trai',
       label_female = N'Chị gái'
 WHERE kind_code = 'SIBLING_OLDER';
GO
