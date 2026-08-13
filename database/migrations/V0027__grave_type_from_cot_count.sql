-- V0027__grave_type_from_cot_count.sql
-- Loại mộ được XÁC ĐỊNH theo số cốt: 1 = Mộ đơn (SINGLE), 2 = Mộ đôi (DOUBLE),
-- ≥3 = Mộ gia tộc (FAMILY). Dữ liệu seed trước đây gán loại ngẫu nhiên, không khớp
-- số cốt → chuẩn hoá lại toàn bộ cho đúng phân loại.
-- (Loại CREMATION/OTHER không còn dùng; mọi mộ nay thuộc 3 loại theo số cốt.)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

UPDATE dbo.Graves
   SET grave_type = CASE
                      WHEN cot_count <= 1 THEN 'SINGLE'
                      WHEN cot_count = 2 THEN 'DOUBLE'
                      ELSE 'FAMILY'
                    END
 WHERE grave_type <> CASE
                       WHEN cot_count <= 1 THEN 'SINGLE'
                       WHEN cot_count = 2 THEN 'DOUBLE'
                       ELSE 'FAMILY'
                     END;
GO
