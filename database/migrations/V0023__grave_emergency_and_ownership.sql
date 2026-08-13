-- V0023__grave_emergency_and_ownership.sql
-- 1. Grave_Emergency_Contacts : liên hệ khẩn cấp ĐỘNG cho phần mộ (nhiều, có ưu tiên)
--    - contact_customer_id (động, SĐT tự theo hồ sơ)  HOẶC  contact_name/phone (nhập tay)
--    - fallback: gọi chủ không được → gọi lần lượt theo priority
-- 2. Grave_Ownership_History  : lịch sử chuyển quyền sở hữu mộ
-- 3. Quyền mới GRAVE_TRANSFER_OWNERSHIP (nhạy cảm, cần lý do) + cấp cho admin
-- Ghi chú: 3 cột liên hệ phẳng cũ trên dbo.Graves GIỮ NGUYÊN ở GĐ1 (entity C# còn map);
--          việc gỡ bỏ + cập nhật entity để dành GĐ2.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

-- ── 1. Liên hệ khẩn cấp động ────────────────────────────────────────────────
CREATE TABLE dbo.Grave_Emergency_Contacts
(
    id                  bigint          IDENTITY(1,1)   NOT NULL,
    grave_id            bigint                          NOT NULL,
    priority            int                             NOT NULL,   -- 1 = gọi trước tiên
    contact_customer_id bigint                          NULL,       -- liên kết KH (động) — SĐT tự theo hồ sơ
    contact_name        nvarchar(200)                   NULL,       -- hoặc nhập tay
    contact_phone       varchar(20)                     NULL,
    relationship_note   nvarchar(100)                   NULL,       -- quan hệ với chủ mộ
    is_active           bit                             NOT NULL,
    created_at          datetime2(3)                    NOT NULL,
    created_by_user_id  bigint                          NULL,
    updated_at          datetime2(3)                    NULL,
    updated_by_user_id  bigint                          NULL,
    row_version         rowversion                      NOT NULL,

    CONSTRAINT PK_Grave_Emergency_Contacts PRIMARY KEY (id),
    CONSTRAINT FK_GEC_grave    FOREIGN KEY (grave_id)            REFERENCES dbo.Graves (id),
    CONSTRAINT FK_GEC_customer FOREIGN KEY (contact_customer_id) REFERENCES dbo.Customers (id),
    CONSTRAINT FK_GEC_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT FK_GEC_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT CK_GEC_priority CHECK (priority >= 1),
    -- phải có ít nhất 1 cách liên hệ: link KH hoặc SĐT nhập tay
    CONSTRAINT CK_GEC_has_target CHECK (contact_customer_id IS NOT NULL OR contact_phone IS NOT NULL)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_GEC_grave_priority ON dbo.Grave_Emergency_Contacts (grave_id, priority);
CREATE NONCLUSTERED INDEX IX_GEC_grave ON dbo.Grave_Emergency_Contacts (grave_id) WHERE is_active = 1;
GO

-- ── 2. Lịch sử chuyển quyền sở hữu ──────────────────────────────────────────
CREATE TABLE dbo.Grave_Ownership_History
(
    id                      bigint          IDENTITY(1,1)   NOT NULL,
    grave_id                bigint                          NOT NULL,
    previous_owner_id       bigint                          NULL,       -- NULL nếu là gán chủ lần đầu
    new_owner_id            bigint                          NOT NULL,
    transfer_type           varchar(20)                     NOT NULL,   -- SALE / INHERITANCE / DEATH / CORRECTION
    reason                  nvarchar(500)                   NULL,
    transferred_at          datetime2(3)                    NOT NULL,
    transferred_by_user_id  bigint                          NULL,
    row_version             rowversion                      NOT NULL,

    CONSTRAINT PK_Grave_Ownership_History PRIMARY KEY (id),
    CONSTRAINT FK_GOH_grave     FOREIGN KEY (grave_id)          REFERENCES dbo.Graves (id),
    CONSTRAINT FK_GOH_prev      FOREIGN KEY (previous_owner_id) REFERENCES dbo.Customers (id),
    CONSTRAINT FK_GOH_new       FOREIGN KEY (new_owner_id)      REFERENCES dbo.Customers (id),
    CONSTRAINT FK_GOH_by_user   FOREIGN KEY (transferred_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT CK_GOH_type CHECK (transfer_type IN ('SALE', 'INHERITANCE', 'DEATH', 'CORRECTION'))
);
GO

CREATE NONCLUSTERED INDEX IX_GOH_grave ON dbo.Grave_Ownership_History (grave_id, transferred_at DESC);
GO

-- ── 3. Quyền chuyển quyền sở hữu ────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'GRAVE_TRANSFER_OWNERSHIP')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('GRAVE_TRANSFER_OWNERSHIP', 'GRAVE', 'TRANSFER', 'GLOBAL', 1, 1, 0, 1,
         N'Chuyển quyền sở hữu phần mộ (bán/thừa kế/qua đời/đính chính). Nhạy cảm, bắt buộc nêu lý do.');
GO

-- Cấp cho admin (đồng bộ pattern V0017)
DECLARE @adminId bigint = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions
                   WHERE user_id = @adminId AND permission_code = 'GRAVE_TRANSFER_OWNERSHIP')
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    VALUES (@adminId, 'GRAVE_TRANSFER_OWNERSHIP', 'GLOBAL', 'ALLOW', SYSUTCDATETIME(), @adminId);
GO
