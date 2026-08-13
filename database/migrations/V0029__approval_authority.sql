-- V0029__approval_authority.sql
-- Thẩm quyền phê duyệt (Approval_Authorities): nguồn dữ liệu độc lập cho "ai được duyệt cái gì".
--   - Không phải engine quy trình; engine tra bảng này qua loại nguồn người duyệt APPROVAL_AUTHORITY.
--   - Một dòng = một người duyệt cho (công ty, phòng ban, cấp), tuỳ chọn giới hạn mã quy trình + ngưỡng tiền,
--     trong một khoảng hiệu lực. Nghỉ phép = đóng dòng cũ + thêm dòng uỷ quyền (delegated_from_user_id).
--   - Seed quyền riêng APPROVAL_AUTHORITY_MANAGE (tách khỏi quyền sửa phòng ban) + cấp cho admin.
-- Ghi chú: dùng GO tách batch. Migrator tự bọc toàn bộ trong 1 transaction.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 1. Bảng Approval_Authorities
-- ══════════════════════════════════════════════════════════════════════════

IF OBJECT_ID('dbo.Approval_Authorities', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Approval_Authorities
    (
        id                      bigint          IDENTITY(1,1)   NOT NULL,
        company_id              bigint                          NOT NULL,   -- công ty áp dụng (D6)
        department_id           bigint                          NOT NULL,   -- phòng ban áp dụng
        process_code            varchar(100)                    NULL,       -- NULL = mọi quy trình
        approver_user_id        bigint                          NOT NULL,   -- người được duyệt
        authority_level         int                             NOT NULL,   -- 1 = TP, 2 = GĐ… (D7)
        min_amount              decimal(18,2)                   NULL,       -- NULL = không giới hạn dưới
        max_amount              decimal(18,2)                   NULL,       -- NULL = không giới hạn trên
        effective_from          datetime2(3)                    NOT NULL,
        effective_to            datetime2(3)                    NULL,       -- NULL = còn hiệu lực
        delegated_from_user_id  bigint                          NULL,       -- dòng uỷ quyền: người uỷ quyền gốc
        status                  varchar(20)                     NOT NULL,   -- ACTIVE / CLOSED
        notes                   nvarchar(2000)                  NULL,
        created_at              datetime2(3)                    NOT NULL,
        created_by_user_id      bigint                          NULL,
        updated_at              datetime2(3)                    NULL,
        updated_by_user_id      bigint                          NULL,
        row_version             rowversion                      NOT NULL,

        CONSTRAINT PK_Approval_Authorities PRIMARY KEY (id),
        CONSTRAINT FK_AA_company_id   FOREIGN KEY (company_id)             REFERENCES dbo.Companies (id),
        CONSTRAINT FK_AA_department_id FOREIGN KEY (department_id)         REFERENCES dbo.Departments (id),
        CONSTRAINT FK_AA_approver      FOREIGN KEY (approver_user_id)      REFERENCES dbo.Users (id),
        CONSTRAINT FK_AA_delegated_from FOREIGN KEY (delegated_from_user_id) REFERENCES dbo.Users (id),
        CONSTRAINT FK_AA_created_by    FOREIGN KEY (created_by_user_id)    REFERENCES dbo.Users (id),
        CONSTRAINT FK_AA_updated_by    FOREIGN KEY (updated_by_user_id)    REFERENCES dbo.Users (id),
        CONSTRAINT CK_AA_authority_level CHECK (authority_level > 0),
        CONSTRAINT CK_AA_status CHECK (status IN ('ACTIVE', 'CLOSED')),
        CONSTRAINT CK_AA_amount_range CHECK (max_amount IS NULL OR min_amount IS NULL OR max_amount >= min_amount),
        CONSTRAINT CK_AA_effective_range CHECK (effective_to IS NULL OR effective_to > effective_from)
    );

    -- Chỉ mục phục vụ resolver: tra theo (công ty, phòng ban, cấp) trên các dòng đang hiệu lực.
    CREATE NONCLUSTERED INDEX IX_AA_company_department_level_status
        ON dbo.Approval_Authorities (company_id, department_id, authority_level, status);

    CREATE NONCLUSTERED INDEX IX_AA_approver_user_id
        ON dbo.Approval_Authorities (approver_user_id);
END
GO

-- ══════════════════════════════════════════════════════════════════════════
-- 2. Seed permission riêng + cấp admin
--    APPROVAL_AUTHORITY_MANAGE tách khỏi quyền sửa phòng ban: ai khai báo người
--    duyệt tiền phải có quyền riêng này (is_sensitive = 1).
-- ══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'APPROVAL_AUTHORITY_MANAGE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('APPROVAL_AUTHORITY_MANAGE', 'WORKFLOW', 'MANAGE', 'GLOBAL', 1, 0, 0, 1, N'Khai báo thẩm quyền phê duyệt (ai được duyệt ở phòng ban/cấp nào).');
GO

DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    SELECT @adminId, p.permission_code, 'GLOBAL', 'ALLOW', SYSUTCDATETIME(), @adminId
    FROM dbo.Permissions p
    WHERE p.permission_code = 'APPROVAL_AUTHORITY_MANAGE'
      AND NOT EXISTS (
          SELECT 1 FROM dbo.User_Individual_Permissions uip
          WHERE uip.user_id = @adminId AND uip.permission_code = p.permission_code
      );
GO
