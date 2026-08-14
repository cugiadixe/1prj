-- V0033__service_type_view_permission.sql
-- Thêm quyền XEM danh mục loại dịch vụ (SERVICE_TYPE_VIEW).
-- Trước đây chỉ có SERVICE_TYPE_MANAGE (quản trị danh mục), nên để CHỌN một gói chăm sóc
-- (liệt kê loại dịch vụ) lại đòi quyền quản trị — vô lý. Endpoint GET nay chấp nhận VIEW hoặc MANAGE.

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE permission_code = 'SERVICE_TYPE_VIEW')
    INSERT INTO dbo.Permissions
        (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
    VALUES
        ('SERVICE_TYPE_VIEW', 'SERVICE', 'VIEW', 'GLOBAL', 0, 0, 0, 1, N'Xem danh mục loại dịch vụ / gói (để chọn khi gán cho khách).');
GO

DECLARE @adminId BIGINT = (SELECT id FROM dbo.Users WHERE employee_code = 'admin');

IF @adminId IS NOT NULL
    INSERT INTO dbo.User_Individual_Permissions (user_id, permission_code, scope_type, grant_type, created_at, created_by_user_id)
    SELECT @adminId, 'SERVICE_TYPE_VIEW', 'GLOBAL', 'ALLOW', SYSUTCDATETIME(), @adminId
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.User_Individual_Permissions uip
        WHERE uip.user_id = @adminId AND uip.permission_code = 'SERVICE_TYPE_VIEW');
GO
