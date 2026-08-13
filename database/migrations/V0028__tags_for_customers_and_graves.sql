-- V0028__tags_for_customers_and_graves.sql
-- Thẻ (hashtag) dùng chung theo TỪ ĐIỂN, TÁCH RIÊNG theo loại đối tượng:
--   dbo.Tags          : danh mục thẻ (tag_type = CUSTOMER | GRAVE), tên duy nhất theo loại, có màu.
--   dbo.Customer_Tags : gắn thẻ (loại CUSTOMER) vào khách hàng.
--   dbo.Grave_Tags    : gắn thẻ (loại GRAVE) vào phần mộ.
-- Quyền TAG_MANAGE: tạo/sửa/gỡ thẻ trong danh mục + gắn/gỡ thẻ vào đối tượng. Cấp cho admin.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

-- ── 1. Danh mục thẻ ─────────────────────────────────────────────────────────
CREATE TABLE dbo.Tags
(
    id                  bigint          IDENTITY(1,1)   NOT NULL,
    tag_type            varchar(20)                     NOT NULL,   -- CUSTOMER | GRAVE
    name                nvarchar(50)                    NOT NULL,   -- nội dung hashtag (không kèm dấu #)
    color               varchar(20)                     NULL,       -- màu preset Ant Design (vd 'blue') hoặc hex
    is_active           bit                             NOT NULL,
    created_at          datetime2(3)                    NOT NULL,
    created_by_user_id  bigint                          NULL,
    updated_at          datetime2(3)                    NULL,
    updated_by_user_id  bigint                          NULL,
    row_version         rowversion                      NOT NULL,

    CONSTRAINT PK_Tags PRIMARY KEY (id),
    CONSTRAINT FK_Tags_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT FK_Tags_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT CK_Tags_type CHECK (tag_type IN ('CUSTOMER', 'GRAVE'))
);
GO

-- Tên thẻ duy nhất theo loại (không phân biệt hoa/thường theo collation mặc định CI của DB).
CREATE UNIQUE NONCLUSTERED INDEX UQ_Tags_type_name ON dbo.Tags (tag_type, name);
GO

-- ── 2. Gắn thẻ vào khách hàng ───────────────────────────────────────────────
CREATE TABLE dbo.Customer_Tags
(
    id                  bigint          IDENTITY(1,1)   NOT NULL,
    customer_id         bigint                          NOT NULL,
    tag_id              bigint                          NOT NULL,
    created_at          datetime2(3)                    NOT NULL,
    created_by_user_id  bigint                          NULL,

    CONSTRAINT PK_Customer_Tags PRIMARY KEY (id),
    CONSTRAINT FK_CustomerTags_customer FOREIGN KEY (customer_id) REFERENCES dbo.Customers (id),
    CONSTRAINT FK_CustomerTags_tag      FOREIGN KEY (tag_id)      REFERENCES dbo.Tags (id),
    CONSTRAINT FK_CustomerTags_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT UQ_Customer_Tags UNIQUE (customer_id, tag_id)
);
GO

CREATE NONCLUSTERED INDEX IX_Customer_Tags_tag ON dbo.Customer_Tags (tag_id, customer_id);
GO

-- ── 3. Gắn thẻ vào phần mộ ──────────────────────────────────────────────────
CREATE TABLE dbo.Grave_Tags
(
    id                  bigint          IDENTITY(1,1)   NOT NULL,
    grave_id            bigint                          NOT NULL,
    tag_id              bigint                          NOT NULL,
    created_at          datetime2(3)                    NOT NULL,
    created_by_user_id  bigint                          NULL,

    CONSTRAINT PK_Grave_Tags PRIMARY KEY (id),
    CONSTRAINT FK_GraveTags_grave FOREIGN KEY (grave_id) REFERENCES dbo.Graves (id),
    CONSTRAINT FK_GraveTags_tag   FOREIGN KEY (tag_id)   REFERENCES dbo.Tags (id),
    CONSTRAINT FK_GraveTags_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT UQ_Grave_Tags UNIQUE (grave_id, tag_id)
);
GO

CREATE NONCLUSTERED INDEX IX_Grave_Tags_tag ON dbo.Grave_Tags (tag_id, grave_id);
GO

-- ── 4. Quyền quản lý thẻ ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'TAG_MANAGE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('TAG_MANAGE', 'TAG', 'MANAGE', 'GLOBAL', 0, 0, 1, 1,
         N'Quản lý thẻ (hashtag): tạo/sửa/gỡ thẻ trong danh mục và gắn/gỡ thẻ vào khách hàng, phần mộ.');
GO

DECLARE @adminId bigint = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions
                   WHERE user_id = @adminId AND permission_code = 'TAG_MANAGE')
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    VALUES (@adminId, 'TAG_MANAGE', 'GLOBAL', 'ALLOW', SYSUTCDATETIME(), @adminId);
GO
