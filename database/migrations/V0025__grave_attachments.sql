-- V0025__grave_attachments.sql
-- Ảnh/tài liệu đính kèm theo phần mộ. File lưu trên ổ đĩa server (mỗi mộ 1 thư mục:
-- storage/graves/{grave_id}/), DB chỉ giữ metadata + tên file lưu (GUID).
--   category: PHOTO (ảnh mộ) · TRANSFER_DOC (văn bản chuyển quyền đã ký) · OTHER
--   ownership_history_id: link tới lần chuyển quyền (nếu là văn bản chuyển quyền)

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

CREATE TABLE dbo.Grave_Attachments
(
    id                      bigint          IDENTITY(1,1)   NOT NULL,
    grave_id                bigint                          NOT NULL,
    category                varchar(30)                     NOT NULL,
    ownership_history_id    bigint                          NULL,
    file_name_original      nvarchar(260)                   NOT NULL,   -- tên gốc để hiển thị/tải
    stored_name             varchar(80)                     NOT NULL,   -- tên lưu trên đĩa = GUID + đuôi
    content_type            varchar(100)                    NOT NULL,
    size_bytes              bigint                          NOT NULL,
    has_thumbnail           bit                             NOT NULL,
    description             nvarchar(500)                   NULL,
    created_at              datetime2(3)                    NOT NULL,
    created_by_user_id      bigint                          NULL,
    updated_at              datetime2(3)                    NULL,
    updated_by_user_id      bigint                          NULL,
    row_version             rowversion                      NOT NULL,

    CONSTRAINT PK_Grave_Attachments PRIMARY KEY (id),
    CONSTRAINT FK_Grave_Attachments_grave FOREIGN KEY (grave_id) REFERENCES dbo.Graves (id),
    CONSTRAINT FK_Grave_Attachments_ownership FOREIGN KEY (ownership_history_id) REFERENCES dbo.Grave_Ownership_History (id),
    CONSTRAINT FK_Grave_Attachments_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT FK_Grave_Attachments_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id),
    CONSTRAINT CK_Grave_Attachments_category CHECK (category IN ('PHOTO', 'TRANSFER_DOC', 'OTHER')),
    CONSTRAINT CK_Grave_Attachments_size CHECK (size_bytes > 0)
);
GO

CREATE NONCLUSTERED INDEX IX_Grave_Attachments_grave ON dbo.Grave_Attachments (grave_id, category);
CREATE NONCLUSTERED INDEX IX_Grave_Attachments_ownership ON dbo.Grave_Attachments (ownership_history_id) WHERE ownership_history_id IS NOT NULL;
GO

-- Quyền quản lý file mộ (tải lên / xóa). Xem file dùng GRAVE_VIEW.
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'GRAVE_ATTACHMENT_MANAGE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('GRAVE_ATTACHMENT_MANAGE', 'GRAVE', 'ATTACHMENT_MANAGE', 'GLOBAL', 0, 0, 0, 1, N'Tải lên / xóa ảnh, tài liệu của phần mộ.');
GO

DECLARE @adminId bigint = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
IF @adminId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions WHERE user_id = @adminId AND permission_code = 'GRAVE_ATTACHMENT_MANAGE')
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    VALUES (@adminId, 'GRAVE_ATTACHMENT_MANAGE', 'GLOBAL', 'ALLOW', SYSUTCDATETIME(), @adminId);
GO
