-- V0034__workflow_condition_fields.sql
-- Danh mục TRƯỜNG ĐIỀU KIỆN cho engine phê duyệt.
--
-- Vì sao cần: bảng Workflow_Conditions đã tồn tại từ V0006 nhưng KHÔNG có code nào đọc lúc chạy,
-- nên mọi luật kiểu "giảm giá > 0 thì mới cần duyệt" phải nằm cứng trong C#. Nhóm 2 xây bộ đánh
-- giá điều kiện; bảng này là phần "ranh giới quản trị": DEV khai báo trước những trường nào của
-- từng quy trình được phép dùng làm điều kiện, admin chỉ CHỌN từ danh sách — không được gõ tên
-- trường tự do, càng không được gõ SQL hay biểu thức (đúng tài liệu approval-workflow-rules.md).
--
-- field_code phải khớp CHÍNH XÁC tên thuộc tính trong payload_json mà module nghiệp vụ ghi ra.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.Workflow_Condition_Fields', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Workflow_Condition_Fields
    (
        id              BIGINT IDENTITY(1,1)    NOT NULL,
        process_code    VARCHAR(100)            NOT NULL,
        field_code      VARCHAR(100)            NOT NULL,
        field_label     NVARCHAR(300)           NOT NULL,
        data_type       VARCHAR(20)             NOT NULL,
        description     NVARCHAR(1000)          NULL,
        is_active       BIT                     NOT NULL CONSTRAINT DF_WCF_is_active DEFAULT (1),
        created_at      DATETIME2(3)            NOT NULL CONSTRAINT DF_WCF_created_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Workflow_Condition_Fields PRIMARY KEY (id),
        CONSTRAINT UQ_WCF_process_field UNIQUE (process_code, field_code),
        CONSTRAINT FK_WCF_process_code FOREIGN KEY (process_code)
            REFERENCES dbo.Business_Process_Catalog (process_code),
        -- Kiểu dữ liệu quyết định cách so sánh: NUMBER so theo số, TEXT so theo chuỗi,
        -- BOOLEAN so đúng/sai, DATE so theo mốc thời gian.
        CONSTRAINT CK_WCF_data_type CHECK (data_type IN ('NUMBER', 'TEXT', 'BOOLEAN', 'DATE'))
    );

    CREATE INDEX IX_WCF_process ON dbo.Workflow_Condition_Fields (process_code) WHERE is_active = 1;
END
GO

-- Seed: các trường thực sự có trong payload_json mà từng module đang ghi ra.
-- (Đối chiếu PayloadJson = JsonSerializer.Serialize(...) trong các service tương ứng.)
MERGE dbo.Workflow_Condition_Fields AS tgt
USING (VALUES
    -- CustomerCarePackageService: new { CustomerId, ServiceTypeId, CotCount }
    ('ASSIGN_CARE_PACKAGE', 'CotCount',      N'Số cốt',              'NUMBER',  N'Số cốt của gói dịch vụ được gán.'),
    ('ASSIGN_CARE_PACKAGE', 'ServiceTypeId', N'Loại dịch vụ (ID)',   'NUMBER',  N'Mã loại dịch vụ / gói chăm sóc.'),
    ('ASSIGN_CARE_PACKAGE', 'CustomerId',    N'Khách hàng (ID)',     'NUMBER',  N'Mã khách hàng được gán gói.'),

    -- CarePackageRequestService: new { CustomerId, ServiceId, TotalAmount }
    ('SELL_CARE_PACKAGE',   'TotalAmount',   N'Tổng tiền',           'NUMBER',  N'Tổng giá trị hợp đồng bán gói.'),
    ('SELL_CARE_PACKAGE',   'ServiceId',     N'Dịch vụ (ID)',        'NUMBER',  N'Mã dịch vụ được bán.'),
    ('SELL_CARE_PACKAGE',   'CustomerId',    N'Khách hàng (ID)',     'NUMBER',  N'Mã khách hàng mua gói.'),

    -- CardReprintRequestService: new { CardId, ReasonCode }
    ('CARD_REPRINT',        'ReasonCode',    N'Lý do in lại',        'TEXT',    N'Mã lý do đề nghị in lại thẻ.'),

    -- CustomerMasterChangeService: new { TargetCustomerId, Reason }
    ('CUSTOMER_MASTER_CHANGE', 'Reason',     N'Lý do thay đổi',      'TEXT',    N'Lý do đề nghị sửa thông tin gốc khách hàng.')
) AS src (process_code, field_code, field_label, data_type, description)
ON  tgt.process_code = src.process_code
AND tgt.field_code   = src.field_code
WHEN NOT MATCHED BY TARGET AND EXISTS (
        SELECT 1 FROM dbo.Business_Process_Catalog b WHERE b.process_code = src.process_code)
    THEN INSERT (process_code, field_code, field_label, data_type, description)
         VALUES (src.process_code, src.field_code, src.field_label, src.data_type, src.description);
GO
