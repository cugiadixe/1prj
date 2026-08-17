-- V0039__card_print_history_and_reprint_workflow.sql
--
-- KHỐI A — "in lần đầu miễn duyệt, in lại (lần 2+) phải duyệt + phí 50.000đ" chạy đúng.
-- (Anh Bách chốt 2026-08-16: 1 cấp duyệt = Giám đốc XN Hà Nội + người thay thế cấu hình được;
--  đếm theo THẺ, số lần in CỘNG DỒN; in lại luôn thu phí 50.000đ.)
--
-- Migration này lo phần DỮ LIỆU/SCHEMA:
--   1. Cột Cards.card_number (SỐ THẺ) — cấp lúc tạo thẻ (luồng tạo thẻ tối thiểu, Pha 2).
--   2. Bảng Card_Print_History (append-only) — nguồn sự thật đếm lần in; UNIQUE 1 dòng INITIAL/thẻ
--      khoá lỗ hai lần in đầu song song.
--   3. Quyền CARD_ISSUE (tạo/cấp thẻ) + cấp cho admin.
--   4. Seed quy trình duyệt CARD_REPRINT (1 bước, khuôn V0031). Người duyệt TẠM = admin
--      (PLACEHOLDER) → cấu hình lại thành Giám đốc XN Hà Nội + người thay thế qua UI cấu hình.
--
-- Luật "lần 2 mới duyệt" nằm ở TẦNG SERVICE (INITIAL → in thẳng; REPRINT → mở workflow), nên
-- binding KHÔNG cần điều kiện — chỉ cần quy trình 1 bước như ASSIGN_CARE_PACKAGE.
-- Phí in lại đã có sẵn: Service_Types code 'IN_THE' = 50.000đ (service sửa để tra đúng mã này).

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 1. Cột số thẻ
-- ══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Cards') AND name = 'card_number')
    ALTER TABLE dbo.Cards ADD card_number nvarchar(50) NULL;
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 2. Bảng Card_Print_History (append-only)
-- ══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Card_Print_History' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Card_Print_History
    (
        id                  bigint          IDENTITY(1,1)   NOT NULL,
        card_id             bigint                          NOT NULL,
        company_id          bigint                          NOT NULL,
        print_sequence      int                             NOT NULL,   -- 1 = in đầu; 2,3… = in lại (cộng dồn)
        print_type          varchar(20)                     NOT NULL,
        reprint_request_id  bigint                          NULL,
        workflow_instance_id bigint                         NULL,
        printed_by_user_id  bigint                          NOT NULL,
        printed_at          datetime2(3)    NOT NULL        CONSTRAINT DF_CPH_printed_at DEFAULT (SYSUTCDATETIME()),
        reason_code         nvarchar(50)                    NULL,
        notes               nvarchar(500)                   NULL,

        CONSTRAINT PK_Card_Print_History PRIMARY KEY (id),
        CONSTRAINT FK_CPH_card FOREIGN KEY (card_id) REFERENCES dbo.Cards (id),
        CONSTRAINT FK_CPH_reprint_request FOREIGN KEY (reprint_request_id) REFERENCES dbo.Card_Reprint_Requests (id),
        CONSTRAINT FK_CPH_printed_by FOREIGN KEY (printed_by_user_id) REFERENCES dbo.Users (id),
        CONSTRAINT CK_CPH_type CHECK (print_type IN ('INITIAL', 'REPRINT'))
    );

    CREATE NONCLUSTERED INDEX IX_CPH_card ON dbo.Card_Print_History (card_id, print_sequence);
    -- 1 thẻ chỉ có ĐÚNG MỘT lần in đầu — khoá lỗ hai lần in đầu song song cùng lọt miễn duyệt.
    CREATE UNIQUE INDEX UQ_CPH_one_initial ON dbo.Card_Print_History (card_id) WHERE print_type = 'INITIAL';
END
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 3. Quyền tạo/cấp thẻ (CARD_ISSUE) + cấp admin
-- ══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'CARD_ISSUE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('CARD_ISSUE', 'CARD', 'CREATE', 'GLOBAL', 0, 0, 0, 1, N'Tạo/cấp thẻ mộ mới (sinh số thẻ) từ phần mộ.');
GO

DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
IF @adminId IS NOT NULL
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    SELECT @adminId, 'CARD_ISSUE', 'GLOBAL', 'ALLOW', SYSUTCDATETIME(), @adminId
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.User_Individual_Permissions uip
        WHERE uip.user_id = @adminId AND uip.permission_code = 'CARD_ISSUE');
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 4. Seed quy trình duyệt in lại (CARD_REPRINT) — 1 bước, khuôn V0031
-- ══════════════════════════════════════════════════════════════════════════
DECLARE @adminId2 BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId2 IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Workflow_Definitions WHERE definition_code = 'CARD_REPRINT_DEFAULT')
BEGIN
    DECLARE @now datetime2(3) = SYSUTCDATETIME();
    DECLARE @defId BIGINT, @verId BIGINT, @stepId BIGINT;

    INSERT INTO dbo.Workflow_Definitions
        (definition_code, definition_name, description, process_code, is_active, created_by, created_at)
    VALUES
        ('CARD_REPRINT_DEFAULT', N'Duyệt in lại thẻ mộ',
         N'In lần đầu miễn duyệt; in lại (lần 2+) cần Giám đốc XN duyệt 1 cấp.',
         'CARD_REPRINT', 1, @adminId2, @now);
    SET @defId = SCOPE_IDENTITY();

    INSERT INTO dbo.Workflow_Definition_Versions
        (workflow_definition_id, version_number, version_status, effective_from, published_at, published_by, created_by, created_at)
    VALUES
        (@defId, 1, 'ACTIVE', @now, @now, @adminId2, @adminId2, @now);
    SET @verId = SCOPE_IDENTITY();

    INSERT INTO dbo.Workflow_Steps
        (workflow_version_id, step_order, step_name, description, is_required, created_at)
    VALUES
        (@verId, 1, N'Giám đốc XN duyệt',
         N'Giám đốc xí nghiệp (hoặc người được cấu hình thay thế khi vắng) duyệt yêu cầu in lại.', 1, @now);
    SET @stepId = SCOPE_IDENTITY();

    -- ⚠️ PLACEHOLDER: người duyệt tạm đặt = admin để pilot chạy được.
    --    Cấu hình lại thành GIÁM ĐỐC XN HÀ NỘI thật + người thay thế qua UI cấu hình quy trình.
    INSERT INTO dbo.Workflow_Step_Approver_Rules
        (workflow_step_id, approver_source_type, approver_source_value, priority, created_at)
    VALUES
        (@stepId, 'SPECIFIC_USER', CAST(@adminId2 AS nvarchar(50)), 0, @now);

    INSERT INTO dbo.Workflow_Bindings
        (workflow_version_id, process_code, scope_type, company_id, priority, effective_from, is_active, created_by, created_at)
    VALUES
        (@verId, 'CARD_REPRINT', 'GLOBAL', NULL, 0, @now, 1, @adminId2, @now);

    PRINT 'Seeded CARD_REPRINT_DEFAULT workflow (def/ver/step/rule/binding).';
END
ELSE
    PRINT 'Skipped CARD_REPRINT workflow seed (admin missing or already seeded).';
GO
