-- V0003__create_security_schema.sql
-- Security Schema Foundation
-- Defines Authentication and Authorization structures for Phase 1B.

-- 1. Security_Audit_Events (Append-only)
CREATE TABLE dbo.Security_Audit_Events (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    event_type VARCHAR(100) NOT NULL,
    actor_user_id BIGINT NULL,
    target_user_id BIGINT NULL,
    company_id BIGINT NULL,
    entity_type VARCHAR(50) NULL,
    entity_id VARCHAR(100) NULL,
    reason NVARCHAR(MAX) NULL,
    before_state_json NVARCHAR(MAX) NULL,
    after_state_json NVARCHAR(MAX) NULL,
    changed_fields NVARCHAR(MAX) NULL,
    correlation_id VARCHAR(100) NULL,
    request_metadata NVARCHAR(MAX) NULL,
    outcome VARCHAR(50) NULL,
    policy_version UNIQUEIDENTIFIER NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_SecurityAudit_Actor FOREIGN KEY (actor_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_SecurityAudit_Target FOREIGN KEY (target_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_SecurityAudit_Company FOREIGN KEY (company_id) REFERENCES dbo.Companies(id)
);
GO

CREATE TRIGGER TR_Security_Audit_Events_PreventUpdateDelete
ON dbo.Security_Audit_Events
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR('UPDATE and DELETE are prohibited on Security_Audit_Events.', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO

-- 2. User_Auth_Accounts
CREATE TABLE dbo.User_Auth_Accounts (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    provider_type VARCHAR(50) NOT NULL,
    provider_subject VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NULL,
    account_status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    failed_login_attempts INT NOT NULL DEFAULT 0,
    lockout_end DATETIME2 NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NULL,
    [row_version] ROWVERSION NOT NULL,
    
    CONSTRAINT FK_UserAuth_Users FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT UQ_UserAuth_Provider UNIQUE (provider_type, provider_subject),
    CONSTRAINT CHK_UserAuth_Status CHECK (account_status IN ('ACTIVE', 'LOCKED', 'DISABLED'))
);
GO

-- 3. Password_History
CREATE TABLE dbo.Password_History (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    account_id BIGINT NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_PasswordHistory_Account FOREIGN KEY (account_id) REFERENCES dbo.User_Auth_Accounts(id)
);
GO
CREATE INDEX IX_PasswordHistory_Account ON dbo.Password_History(account_id);
GO

-- 4. Refresh_Tokens
CREATE TABLE dbo.Refresh_Tokens (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    account_id BIGINT NOT NULL,
    token_hash VARCHAR(64) NOT NULL,
    family_id UNIQUEIDENTIFIER NOT NULL,
    expires_at DATETIME2 NOT NULL,
    is_revoked BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_RefreshTokens_Account FOREIGN KEY (account_id) REFERENCES dbo.User_Auth_Accounts(id),
    CONSTRAINT UQ_RefreshTokens_Hash UNIQUE (token_hash)
);
GO
CREATE INDEX IX_RefreshTokens_Family ON dbo.Refresh_Tokens(family_id);
GO

-- 5. Permissions
CREATE TABLE dbo.Permissions (
    permission_code VARCHAR(100) NOT NULL PRIMARY KEY,
    description NVARCHAR(500) NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
INSERT INTO dbo.Permissions (permission_code, description) VALUES
('SECURITY_ROLE_VIEW', 'View global roles'),
('SECURITY_ROLE_MANAGE', 'Manage global roles'),
('SECURITY_ASSIGNMENT_MANAGE', 'Manage role/permission assignments'),
('SECURITY_ADMIN_GROUP_VIEW', 'View admin groups'),
('SECURITY_ADMIN_GROUP_MANAGE', 'Manage admin groups'),
('SECURITY_ACCOUNT_MANAGE', 'Manage security accounts (lock/unlock)'),
('SECURITY_AUDIT_VIEW', 'View security audit logs');
GO

-- 6. Roles
CREATE TABLE dbo.Roles (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    role_code VARCHAR(50) NOT NULL,
    description NVARCHAR(500) NULL,
    company_id BIGINT NULL,
    scope_type VARCHAR(20) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [row_version] ROWVERSION NOT NULL,
    
    CONSTRAINT UQ_Roles_RoleCode UNIQUE (role_code),
    CONSTRAINT FK_Roles_Company FOREIGN KEY (company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT CHK_Roles_Scope CHECK (scope_type IN ('GLOBAL', 'COMPANY')),
    CONSTRAINT CHK_Roles_ScopeCompany CHECK (
        (scope_type = 'GLOBAL' AND company_id IS NULL) OR 
        (scope_type = 'COMPANY' AND company_id IS NOT NULL)
    )
);
GO

-- 7. Role_Permissions
CREATE TABLE dbo.Role_Permissions (
    role_id BIGINT NOT NULL,
    permission_code VARCHAR(100) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    
    PRIMARY KEY (role_id, permission_code),
    CONSTRAINT FK_RolePerms_Role FOREIGN KEY (role_id) REFERENCES dbo.Roles(id),
    CONSTRAINT FK_RolePerms_Perm FOREIGN KEY (permission_code) REFERENCES dbo.Permissions(permission_code)
);
GO

-- 8. Department_Permissions
CREATE TABLE dbo.Department_Permissions (
    department_id BIGINT NOT NULL,
    permission_code VARCHAR(100) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    
    PRIMARY KEY (department_id, permission_code),
    CONSTRAINT FK_DeptPerms_Dept FOREIGN KEY (department_id) REFERENCES dbo.Departments(id),
    CONSTRAINT FK_DeptPerms_Perm FOREIGN KEY (permission_code) REFERENCES dbo.Permissions(permission_code)
);
GO

-- 9. Admin_Groups
CREATE TABLE dbo.Admin_Groups (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    group_code VARCHAR(50) NOT NULL,
    description NVARCHAR(500) NULL,
    company_id BIGINT NULL,
    scope_type VARCHAR(20) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [row_version] ROWVERSION NOT NULL,
    
    CONSTRAINT UQ_AdminGroups_GroupCode UNIQUE (group_code),
    CONSTRAINT FK_AdminGroups_Company FOREIGN KEY (company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT CHK_AdminGroups_Scope CHECK (scope_type IN ('GLOBAL', 'COMPANY')),
    CONSTRAINT CHK_AdminGroups_ScopeCompany CHECK (
        (scope_type = 'GLOBAL' AND company_id IS NULL) OR 
        (scope_type = 'COMPANY' AND company_id IS NOT NULL)
    )
);
GO

-- 10. Admin_Group_Permissions
CREATE TABLE dbo.Admin_Group_Permissions (
    admin_group_id BIGINT NOT NULL,
    permission_code VARCHAR(100) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    
    PRIMARY KEY (admin_group_id, permission_code),
    CONSTRAINT FK_GroupPerms_Group FOREIGN KEY (admin_group_id) REFERENCES dbo.Admin_Groups(id),
    CONSTRAINT FK_GroupPerms_Perm FOREIGN KEY (permission_code) REFERENCES dbo.Permissions(permission_code)
);
GO

-- 11. User_Role_Assignments
CREATE TABLE dbo.User_Role_Assignments (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    assignment_status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    effective_from DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    effective_to DATETIME2 NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_UserRole_User FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserRole_Role FOREIGN KEY (role_id) REFERENCES dbo.Roles(id),
    CONSTRAINT CHK_UserRole_Dates CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT CHK_UserRole_Status CHECK (assignment_status IN ('ACTIVE', 'CLOSED'))
);
GO
-- Filtered index to prevent active overlaps
CREATE UNIQUE INDEX UQ_UserRole_ActiveOverlap ON dbo.User_Role_Assignments(user_id, role_id) WHERE assignment_status = 'ACTIVE';
GO

-- 12. User_Individual_Permissions
CREATE TABLE dbo.User_Individual_Permissions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    company_id BIGINT NULL,
    permission_code VARCHAR(100) NOT NULL,
    grant_type VARCHAR(10) NOT NULL,
    assignment_status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    effective_from DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    effective_to DATETIME2 NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_UserIndvPerm_User FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserIndvPerm_Company FOREIGN KEY (company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT FK_UserIndvPerm_Perm FOREIGN KEY (permission_code) REFERENCES dbo.Permissions(permission_code),
    CONSTRAINT CHK_UserIndvPerm_GrantType CHECK (grant_type IN ('ALLOW', 'DENY')),
    CONSTRAINT CHK_UserIndvPerm_Status CHECK (assignment_status IN ('ACTIVE', 'CLOSED')),
    CONSTRAINT CHK_UserIndvPerm_Dates CHECK (effective_to IS NULL OR effective_to > effective_from)
);
GO
-- We prevent multiple active grants of the same type for the same user/company/permission combination. 
-- For GLOBAL, company_id IS NULL so we index differently if we have to, but since company_id can be NULL, we can just let it overlap if SQL Server ignores NULLs in unique constraints, but actually SQL Server unique indexes ONLY allow one NULL. 
CREATE UNIQUE INDEX UQ_UserIndvPerm_ActiveOverlap_Company ON dbo.User_Individual_Permissions(user_id, company_id, permission_code, grant_type) WHERE assignment_status = 'ACTIVE' AND company_id IS NOT NULL;
CREATE UNIQUE INDEX UQ_UserIndvPerm_ActiveOverlap_Global ON dbo.User_Individual_Permissions(user_id, permission_code, grant_type) WHERE assignment_status = 'ACTIVE' AND company_id IS NULL;
GO

-- 13. User_Admin_Group_Assignments
CREATE TABLE dbo.User_Admin_Group_Assignments (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT NOT NULL,
    admin_group_id BIGINT NOT NULL,
    assignment_status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    effective_from DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    effective_to DATETIME2 NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT FK_UserGroup_User FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserGroup_Group FOREIGN KEY (admin_group_id) REFERENCES dbo.Admin_Groups(id),
    CONSTRAINT CHK_UserGroup_Status CHECK (assignment_status IN ('ACTIVE', 'CLOSED')),
    CONSTRAINT CHK_UserGroup_Dates CHECK (effective_to IS NULL OR effective_to > effective_from)
);
GO
CREATE UNIQUE INDEX UQ_UserGroup_ActiveOverlap ON dbo.User_Admin_Group_Assignments(user_id, admin_group_id) WHERE assignment_status = 'ACTIVE';
GO

-- 14. Singletons
CREATE TABLE dbo.Authorization_Policy_State (
    id INT NOT NULL PRIMARY KEY CHECK (id = 1),
    policy_version UNIQUEIDENTIFIER NOT NULL,
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
INSERT INTO dbo.Authorization_Policy_State (id, policy_version) VALUES (1, NEWID());
GO

CREATE TABLE dbo.Security_Bootstrap_State (
    id INT NOT NULL PRIMARY KEY CHECK (id = 1),
    is_bootstrapped BIT NOT NULL DEFAULT 0,
    bootstrapped_at DATETIME2 NULL
);
GO
INSERT INTO dbo.Security_Bootstrap_State (id) VALUES (1);
GO

-- 15. View
CREATE VIEW dbo.vw_SECURITY_AUDIT_VIEW AS
SELECT 
    id, event_type, actor_user_id, target_user_id, company_id, 
    entity_type, entity_id, reason, before_state_json, after_state_json, 
    changed_fields, correlation_id, request_metadata, outcome, policy_version, created_at
FROM dbo.Security_Audit_Events;
GO
