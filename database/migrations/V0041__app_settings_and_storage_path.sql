-- V0041__app_settings_and_storage_path.sql
--
-- Cho phép CẤU HÌNH RUNTIME (không sửa appsettings + khởi động lại). Trước mắt phục vụ:
-- đường dẫn thư mục lưu file đính kèm mộ (FileStorage:BasePath). Chỉ người có quyền
-- SYSTEM_SETTING_MANAGE mới đọc/sửa (anh Bách: chỉ admin).
--
-- Bảng App_Settings là key/value chung (tái dùng cho cấu hình hệ thống về sau). Không seed giá trị
-- FileStorage:BasePath -> để trống nghĩa là dùng mặc định từ appsettings (giữ nguyên hành vi cũ).

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

-- 1. Bảng cấu hình hệ thống (key/value)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'App_Settings' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.App_Settings
    (
        id                  bigint          IDENTITY(1,1)   NOT NULL,
        setting_key         nvarchar(100)                   NOT NULL,
        setting_value       nvarchar(1000)                  NULL,
        updated_at          datetime2(3)    NOT NULL        CONSTRAINT DF_App_Settings_updated_at DEFAULT (SYSUTCDATETIME()),
        updated_by_user_id  bigint                          NULL,
        row_version         rowversion                      NOT NULL,

        CONSTRAINT PK_App_Settings PRIMARY KEY (id),
        CONSTRAINT UQ_App_Settings_key UNIQUE (setting_key),
        CONSTRAINT FK_App_Settings_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id)
    );
END
GO

-- 2. Quyền quản trị cấu hình hệ thống + cấp cho admin (holder của SECURITY_ADMIN_MANAGE)
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'SYSTEM_SETTING_MANAGE')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('SYSTEM_SETTING_MANAGE', 'SYSTEM', 'MANAGE', 'GLOBAL', 1, 0, 0, 1,
         N'Xem/sửa cấu hình hệ thống (vd đường dẫn thư mục lưu file đính kèm). Chỉ quản trị viên.');
GO

DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');
IF @adminId IS NOT NULL
    INSERT INTO dbo.User_Individual_Permissions
        (user_id, permission_code, scope_type, grant_type, assignment_status, effective_from, created_at, created_by_user_id)
    SELECT @adminId, 'SYSTEM_SETTING_MANAGE', 'GLOBAL', 'ALLOW', 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME(), @adminId
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.User_Individual_Permissions uip
        WHERE uip.user_id = @adminId AND uip.permission_code = 'SYSTEM_SETTING_MANAGE');
GO

-- Cấp thêm cho MỌI người đang có SECURITY_ADMIN_MANAGE (vai trò/nhóm) để "admin" nào cũng chỉnh được
INSERT INTO dbo.Role_Permissions (role_id, permission_code, created_at, created_by_user_id)
SELECT rp.role_id, 'SYSTEM_SETTING_MANAGE', SYSUTCDATETIME(), rp.created_by_user_id
FROM dbo.Role_Permissions rp
WHERE rp.permission_code = 'SECURITY_ADMIN_MANAGE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Role_Permissions x WHERE x.role_id = rp.role_id AND x.permission_code = 'SYSTEM_SETTING_MANAGE');
GO

INSERT INTO dbo.Admin_Group_Permissions (admin_group_id, permission_code, created_at, created_by_user_id)
SELECT gp.admin_group_id, 'SYSTEM_SETTING_MANAGE', SYSUTCDATETIME(), gp.created_by_user_id
FROM dbo.Admin_Group_Permissions gp
WHERE gp.permission_code = 'SECURITY_ADMIN_MANAGE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions x WHERE x.admin_group_id = gp.admin_group_id AND x.permission_code = 'SYSTEM_SETTING_MANAGE');
GO

-- 3. Bump policy_version để cache quyền nạp lại ngay
UPDATE dbo.Authorization_Policy_State SET policy_version = policy_version + 1, updated_at = SYSUTCDATETIME() WHERE id = 1;
GO
