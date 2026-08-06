-- V0004__seed_security_admin_manage_permission.sql
-- Phase 1B.1-F-B0 corrective backfill: SECURITY_ADMIN_MANAGE permission.

INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('SECURITY_ADMIN_MANAGE', 'SECURITY', 'ADMIN_MANAGE', 'GLOBAL', 1, 1, 0, 1, N'Manage security administration configuration (Roles, AdminGroups, Permissions, UserAssignments, DepartmentPermissions, EffectivePermissions).');
