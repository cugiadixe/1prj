-- V0035__seed_missing_processes_and_discount_field.sql
--
-- 1) Bổ sung 2 mã quy trình ĐANG DÙNG TRONG CODE nhưng chưa có trong danh mục.
--    Business_Process_Catalog là bảng cha (khoá ngoại) của Workflow_Definitions và
--    Workflow_Bindings, nên thiếu dòng ở đây là KHÔNG THỂ tạo định nghĩa hay liên kết —
--    quy trình xem như bị khoá cứng dù code đã sẵn sàng:
--      - CARD_REPRINT              (Cards/Services/CardReprintRequestService.cs)
--      - CUSTOMER_MERGE_DUPLICATE  (Customers/Handlers/CustomerMergeExecutionHandler.cs)
--
-- 2) Bổ sung trường điều kiện DiscountAmount cho SELL_CARE_PACKAGE, để luật
--    "có giảm giá thì mới cần duyệt" khai báo được bằng cấu hình thay vì nằm cứng trong C#
--    (CarePackageRequest.EvaluateApprovalRequirement).
--
-- 3) Bổ sung trường điều kiện cho 2 quy trình vừa thêm (giờ khoá ngoại mới cho phép).

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

MERGE dbo.Business_Process_Catalog AS tgt
USING (VALUES
    ('CARD_REPRINT',             N'In lại thẻ',                  N'Duyệt yêu cầu in lại thẻ cho khách hàng.', 1),
    ('CUSTOMER_MERGE_DUPLICATE', N'Gộp khách hàng trùng',        N'Duyệt việc gộp hai hồ sơ khách hàng trùng nhau.', 1)
) AS src (process_code, process_name, description, is_approval_required)
ON tgt.process_code = src.process_code
-- created_at ở bảng này KHÔNG có giá trị mặc định (V0006), phải điền tay.
WHEN NOT MATCHED BY TARGET
    THEN INSERT (process_code, process_name, description, is_approval_required, is_active, created_at)
         VALUES (src.process_code, src.process_name, src.description, src.is_approval_required, 1, SYSUTCDATETIME());
GO

MERGE dbo.Workflow_Condition_Fields AS tgt
USING (VALUES
    -- Luật giảm giá: chuyển từ C# ra cấu hình.
    ('SELL_CARE_PACKAGE', 'DiscountAmount', N'Số tiền giảm giá', 'NUMBER',
        N'Số tiền giảm cho hợp đồng. Dùng để khai báo luật "có giảm giá thì phải qua phê duyệt".'),

    -- CardReprintRequestService: new { CardId, ReasonCode }
    ('CARD_REPRINT', 'ReasonCode', N'Lý do in lại', 'TEXT',  N'Mã lý do đề nghị in lại thẻ.'),
    ('CARD_REPRINT', 'CardId',     N'Thẻ (ID)',     'NUMBER', N'Mã thẻ cần in lại.'),

    -- Gộp khách hàng trùng.
    ('CUSTOMER_MERGE_DUPLICATE', 'SourceCustomerId', N'Khách hàng nguồn (ID)', 'NUMBER', N'Hồ sơ sẽ bị gộp vào hồ sơ đích.'),
    ('CUSTOMER_MERGE_DUPLICATE', 'TargetCustomerId', N'Khách hàng đích (ID)',  'NUMBER', N'Hồ sơ được giữ lại sau khi gộp.')
) AS src (process_code, field_code, field_label, data_type, description)
ON  tgt.process_code = src.process_code
AND tgt.field_code   = src.field_code
WHEN NOT MATCHED BY TARGET AND EXISTS (
        SELECT 1 FROM dbo.Business_Process_Catalog b WHERE b.process_code = src.process_code)
    THEN INSERT (process_code, field_code, field_label, data_type, description)
         VALUES (src.process_code, src.field_code, src.field_label, src.data_type, src.description);
GO
