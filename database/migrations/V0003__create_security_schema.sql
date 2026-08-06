-- V0003__create_security_schema.sql
-- Phase 1B.1-A security database foundation.
-- Application audit writers introduced in a later authorized slice must sanitize
-- JSON payloads so passwords, password hashes, raw tokens, signing keys, secrets,
-- file bytes, and permanent signed URLs are never persisted here.

CREATE TABLE dbo.User_Auth_Accounts (
    id bigint IDENTITY(1,1) NOT NULL,
    user_id bigint NOT NULL,
    provider_type varchar(30) NOT NULL,
    provider_subject varchar(200) NOT NULL,
    password_hash varchar(500) NULL,
    auth_account_status varchar(30) NOT NULL
        CONSTRAINT DF_UserAuthAccounts_Status DEFAULT ('ACTIVE'),
    failed_attempt_count int NOT NULL
        CONSTRAINT DF_UserAuthAccounts_FailedAttemptCount DEFAULT (0),
    lockout_end datetime2(3) NULL,
    must_change_password bit NOT NULL
        CONSTRAINT DF_UserAuthAccounts_MustChangePassword DEFAULT (0),
    temporary_password_expires_at datetime2(3) NULL,
    security_stamp uniqueidentifier NOT NULL
        CONSTRAINT DF_UserAuthAccounts_SecurityStamp DEFAULT (NEWID()),
    sessions_invalidated_at datetime2(3) NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_UserAuthAccounts_CreatedAt DEFAULT (SYSUTCDATETIME()),
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_User_Auth_Accounts PRIMARY KEY (id),
    CONSTRAINT UQ_UserAuthAccounts_ProviderSubject UNIQUE (provider_type, provider_subject),
    CONSTRAINT FK_UserAuthAccounts_User FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserAuthAccounts_CreatedBy FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserAuthAccounts_UpdatedBy FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT CK_UserAuthAccounts_Status CHECK (auth_account_status IN ('ACTIVE', 'LOCKED', 'DISABLED')),
    CONSTRAINT CK_UserAuthAccounts_FailedAttemptCount CHECK (failed_attempt_count >= 0),
    CONSTRAINT CK_UserAuthAccounts_TemporaryPassword CHECK (
        temporary_password_expires_at IS NULL OR must_change_password = 1
    )
);
GO

CREATE INDEX IX_UserAuthAccounts_UserId
    ON dbo.User_Auth_Accounts(user_id);
GO

CREATE TABLE dbo.Password_History (
    id bigint IDENTITY(1,1) NOT NULL,
    account_id bigint NOT NULL,
    password_hash varchar(500) NOT NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_PasswordHistory_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Password_History PRIMARY KEY (id),
    CONSTRAINT FK_PasswordHistory_Account FOREIGN KEY (account_id) REFERENCES dbo.User_Auth_Accounts(id)
);
GO

CREATE INDEX IX_PasswordHistory_Account_CreatedAt
    ON dbo.Password_History(account_id, created_at DESC, id DESC)
    INCLUDE (password_hash);
GO

CREATE TRIGGER dbo.TR_Password_History_AppendOnly
ON dbo.Password_History
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51010, 'Password_History is append-only; UPDATE and DELETE are prohibited.', 1;
END;
GO

CREATE TABLE dbo.Refresh_Tokens (
    id bigint IDENTITY(1,1) NOT NULL,
    account_id bigint NOT NULL,
    token_hash char(64) NOT NULL,
    family_id uniqueidentifier NOT NULL,
    session_id uniqueidentifier NOT NULL,
    issued_at datetime2(3) NOT NULL
        CONSTRAINT DF_RefreshTokens_IssuedAt DEFAULT (SYSUTCDATETIME()),
    expires_at datetime2(3) NOT NULL,
    used_at datetime2(3) NULL,
    revoked_at datetime2(3) NULL,
    revoke_reason varchar(100) NULL,
    replaced_by_token_id bigint NULL,
    reuse_detected_at datetime2(3) NULL,
    created_ip_address varchar(45) NULL,
    created_user_agent nvarchar(500) NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_Refresh_Tokens PRIMARY KEY (id),
    CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (token_hash),
    CONSTRAINT FK_RefreshTokens_Account FOREIGN KEY (account_id) REFERENCES dbo.User_Auth_Accounts(id),
    CONSTRAINT FK_RefreshTokens_ReplacedBy FOREIGN KEY (replaced_by_token_id) REFERENCES dbo.Refresh_Tokens(id),
    CONSTRAINT CK_RefreshTokens_Expiry CHECK (expires_at > issued_at),
    CONSTRAINT CK_RefreshTokens_Replacement CHECK (replaced_by_token_id IS NULL OR replaced_by_token_id <> id)
);
GO

CREATE INDEX IX_RefreshTokens_AccountId
    ON dbo.Refresh_Tokens(account_id);
GO

CREATE INDEX IX_RefreshTokens_FamilyId
    ON dbo.Refresh_Tokens(family_id, issued_at DESC);
GO

CREATE INDEX IX_RefreshTokens_SessionId
    ON dbo.Refresh_Tokens(session_id, issued_at DESC);
GO

CREATE TABLE dbo.Permissions (
    permission_code varchar(100) NOT NULL,
    module_code varchar(50) NOT NULL,
    action_code varchar(50) NOT NULL,
    data_scope varchar(30) NOT NULL,
    is_sensitive bit NOT NULL
        CONSTRAINT DF_Permissions_IsSensitive DEFAULT (0),
    requires_reason bit NOT NULL
        CONSTRAINT DF_Permissions_RequiresReason DEFAULT (0),
    is_delegable bit NOT NULL
        CONSTRAINT DF_Permissions_IsDelegable DEFAULT (0),
    is_active bit NOT NULL
        CONSTRAINT DF_Permissions_IsActive DEFAULT (1),
    description nvarchar(500) NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_Permissions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    updated_at datetime2(3) NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_Permissions PRIMARY KEY (permission_code),
    CONSTRAINT CK_Permissions_DataScope CHECK (data_scope IN ('GLOBAL', 'COMPANY'))
);
GO

INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('ORGANIZATION_COMPANY_VIEW',       'ORGANIZATION', 'COMPANY_VIEW',       'GLOBAL',  0, 0, 0, 1, N'View companies.'),
    ('ORGANIZATION_COMPANY_MANAGE',     'ORGANIZATION', 'COMPANY_MANAGE',     'GLOBAL',  1, 1, 0, 1, N'Manage companies.'),
    ('ORGANIZATION_DEPARTMENT_VIEW',    'ORGANIZATION', 'DEPARTMENT_VIEW',    'GLOBAL',  0, 0, 0, 1, N'View departments.'),
    ('ORGANIZATION_DEPARTMENT_MANAGE',  'ORGANIZATION', 'DEPARTMENT_MANAGE',  'GLOBAL',  1, 1, 0, 1, N'Manage departments.'),
    ('SECURITY_USER_VIEW',              'SECURITY',     'USER_VIEW',          'GLOBAL',  1, 0, 0, 1, N'View security user administration data.'),
    ('SECURITY_USER_MANAGE',            'SECURITY',     'USER_MANAGE',        'GLOBAL',  1, 1, 0, 1, N'Manage security user administration data.'),
    ('SECURITY_ASSIGNMENT_MANAGE',      'SECURITY',     'ASSIGNMENT_MANAGE',  'COMPANY', 1, 1, 0, 1, N'Manage scoped security assignments.'),
    ('SECURITY_ROLE_VIEW',              'SECURITY',     'ROLE_VIEW',          'GLOBAL',  1, 0, 0, 1, N'View security roles.'),
    ('SECURITY_ROLE_MANAGE',            'SECURITY',     'ROLE_MANAGE',        'GLOBAL',  1, 1, 0, 1, N'Manage security roles.'),
    ('SECURITY_PERMISSION_VIEW',        'SECURITY',     'PERMISSION_VIEW',    'GLOBAL',  1, 0, 0, 1, N'View the security permission catalog.'),
    ('SECURITY_PERMISSION_MANAGE',      'SECURITY',     'PERMISSION_MANAGE',  'GLOBAL',  1, 1, 0, 1, N'Manage the security permission catalog.'),
    ('SECURITY_ACCOUNT_MANAGE',         'SECURITY',     'ACCOUNT_MANAGE',     'GLOBAL',  1, 1, 0, 1, N'Manage authentication accounts and sessions.'),
    ('SECURITY_ADMIN_GROUP_VIEW',       'SECURITY',     'ADMIN_GROUP_VIEW',   'GLOBAL',  1, 0, 0, 1, N'View security Admin Groups.'),
    ('SECURITY_ADMIN_GROUP_MANAGE',     'SECURITY',     'ADMIN_GROUP_MANAGE', 'GLOBAL',  1, 1, 0, 1, N'Manage security Admin Groups.'),
    ('SECURITY_AUDIT_VIEW',             'SECURITY',     'AUDIT_VIEW',         'GLOBAL',  1, 1, 0, 1, N'View authentication, authorization, and security administration audit events.');
GO

CREATE TRIGGER dbo.TR_Permissions_PreventDelete
ON dbo.Permissions
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51011, 'Released permission codes may not be deleted; deactivate the permission instead.', 1;
END;
GO

CREATE TRIGGER dbo.TR_Permissions_PreventCodeChange
ON dbo.Permissions
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(permission_code)
        THROW 51012, 'Released permission_code values are immutable.', 1;
END;
GO

CREATE TABLE dbo.Roles (
    id bigint IDENTITY(1,1) NOT NULL,
    role_code varchar(100) NOT NULL,
    name nvarchar(200) NOT NULL,
    description nvarchar(500) NULL,
    scope_type varchar(30) NOT NULL,
    company_id bigint NULL,
    is_active bit NOT NULL
        CONSTRAINT DF_Roles_IsActive DEFAULT (1),
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_Roles_CreatedAt DEFAULT (SYSUTCDATETIME()),
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (id),
    CONSTRAINT UQ_Roles_RoleCode UNIQUE (role_code),
    CONSTRAINT FK_Roles_Company FOREIGN KEY (company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT FK_Roles_CreatedBy FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_Roles_UpdatedBy FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT CK_Roles_ScopeType CHECK (scope_type IN ('GLOBAL', 'COMPANY')),
    CONSTRAINT CK_Roles_ScopeCompany CHECK (
        (scope_type = 'GLOBAL' AND company_id IS NULL)
        OR (scope_type = 'COMPANY' AND company_id IS NOT NULL)
    )
);
GO

CREATE TABLE dbo.Role_Permissions (
    role_id bigint NOT NULL,
    permission_code varchar(100) NOT NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_RolePermissions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    created_by_user_id bigint NULL,
    CONSTRAINT PK_Role_Permissions PRIMARY KEY (role_id, permission_code),
    CONSTRAINT FK_RolePermissions_Role FOREIGN KEY (role_id) REFERENCES dbo.Roles(id),
    CONSTRAINT FK_RolePermissions_Permission FOREIGN KEY (permission_code) REFERENCES dbo.Permissions(permission_code),
    CONSTRAINT FK_RolePermissions_CreatedBy FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id)
);
GO

CREATE TABLE dbo.Department_Permissions (
    department_id bigint NOT NULL,
    permission_code varchar(100) NOT NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_DepartmentPermissions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    created_by_user_id bigint NULL,
    CONSTRAINT PK_Department_Permissions PRIMARY KEY (department_id, permission_code),
    CONSTRAINT FK_DepartmentPermissions_Department FOREIGN KEY (department_id) REFERENCES dbo.Departments(id),
    CONSTRAINT FK_DepartmentPermissions_Permission FOREIGN KEY (permission_code) REFERENCES dbo.Permissions(permission_code),
    CONSTRAINT FK_DepartmentPermissions_CreatedBy FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id)
);
GO

CREATE TABLE dbo.User_Role_Assignments (
    id bigint IDENTITY(1,1) NOT NULL,
    user_id bigint NOT NULL,
    role_id bigint NOT NULL,
    assignment_status varchar(30) NOT NULL
        CONSTRAINT DF_UserRoleAssignments_Status DEFAULT ('ACTIVE'),
    effective_from datetime2(3) NOT NULL
        CONSTRAINT DF_UserRoleAssignments_EffectiveFrom DEFAULT (SYSUTCDATETIME()),
    effective_to datetime2(3) NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_UserRoleAssignments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_User_Role_Assignments PRIMARY KEY (id),
    CONSTRAINT FK_UserRoleAssignments_User FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserRoleAssignments_Role FOREIGN KEY (role_id) REFERENCES dbo.Roles(id),
    CONSTRAINT FK_UserRoleAssignments_CreatedBy FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserRoleAssignments_UpdatedBy FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT CK_UserRoleAssignments_Status CHECK (assignment_status IN ('SCHEDULED', 'ACTIVE', 'CLOSED', 'REVOKED')),
    CONSTRAINT CK_UserRoleAssignments_EffectiveDates CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_UserRoleAssignments_StatusDates CHECK (
        assignment_status IN ('SCHEDULED', 'ACTIVE')
        OR (assignment_status IN ('CLOSED', 'REVOKED') AND effective_to IS NOT NULL)
    )
);
GO

CREATE INDEX IX_UserRoleAssignments_OverlapLookup
    ON dbo.User_Role_Assignments(user_id, role_id, effective_from, effective_to);
GO

CREATE UNIQUE INDEX UQ_UserRoleAssignments_CurrentActive
    ON dbo.User_Role_Assignments(user_id, role_id)
    WHERE assignment_status = 'ACTIVE' AND effective_to IS NULL;
GO

CREATE TRIGGER dbo.TR_User_Role_Assignments_PreventOverlap
ON dbo.User_Role_Assignments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted AS candidate
        INNER JOIN dbo.User_Role_Assignments AS existing WITH (UPDLOCK, HOLDLOCK)
            ON existing.user_id = candidate.user_id
            AND existing.role_id = candidate.role_id
            AND existing.id <> candidate.id
            AND candidate.effective_from < COALESCE(existing.effective_to, CONVERT(datetime2(3), '9999-12-31T23:59:59.997'))
            AND existing.effective_from < COALESCE(candidate.effective_to, CONVERT(datetime2(3), '9999-12-31T23:59:59.997'))
    )
    BEGIN
        THROW 51020, 'User role assignment effective periods may not overlap.', 1;
    END;
END;
GO

CREATE TABLE dbo.User_Individual_Permissions (
    id bigint IDENTITY(1,1) NOT NULL,
    user_id bigint NOT NULL,
    permission_code varchar(100) NOT NULL,
    scope_type varchar(30) NOT NULL,
    company_id bigint NULL,
    grant_type varchar(10) NOT NULL,
    assignment_status varchar(30) NOT NULL
        CONSTRAINT DF_UserIndividualPermissions_Status DEFAULT ('ACTIVE'),
    effective_from datetime2(3) NOT NULL
        CONSTRAINT DF_UserIndividualPermissions_EffectiveFrom DEFAULT (SYSUTCDATETIME()),
    effective_to datetime2(3) NULL,
    reason nvarchar(500) NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_UserIndividualPermissions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_User_Individual_Permissions PRIMARY KEY (id),
    CONSTRAINT FK_UserIndividualPermissions_User FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserIndividualPermissions_Permission FOREIGN KEY (permission_code) REFERENCES dbo.Permissions(permission_code),
    CONSTRAINT FK_UserIndividualPermissions_Company FOREIGN KEY (company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT FK_UserIndividualPermissions_CreatedBy FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserIndividualPermissions_UpdatedBy FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT CK_UserIndividualPermissions_ScopeType CHECK (scope_type IN ('GLOBAL', 'COMPANY')),
    CONSTRAINT CK_UserIndividualPermissions_ScopeCompany CHECK (
        (scope_type = 'GLOBAL' AND company_id IS NULL)
        OR (scope_type = 'COMPANY' AND company_id IS NOT NULL)
    ),
    CONSTRAINT CK_UserIndividualPermissions_GrantType CHECK (grant_type IN ('ALLOW', 'DENY')),
    CONSTRAINT CK_UserIndividualPermissions_Status CHECK (assignment_status IN ('SCHEDULED', 'ACTIVE', 'CLOSED', 'REVOKED')),
    CONSTRAINT CK_UserIndividualPermissions_EffectiveDates CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_UserIndividualPermissions_StatusDates CHECK (
        assignment_status IN ('SCHEDULED', 'ACTIVE')
        OR (assignment_status IN ('CLOSED', 'REVOKED') AND effective_to IS NOT NULL)
    )
);
GO

CREATE INDEX IX_UserIndividualPermissions_OverlapLookup
    ON dbo.User_Individual_Permissions(user_id, permission_code, scope_type, company_id, grant_type, effective_from, effective_to);
GO

CREATE UNIQUE INDEX UQ_UserIndividualPermissions_CurrentActiveCompany
    ON dbo.User_Individual_Permissions(user_id, permission_code, scope_type, company_id, grant_type)
    WHERE assignment_status = 'ACTIVE' AND effective_to IS NULL AND company_id IS NOT NULL;
GO

CREATE UNIQUE INDEX UQ_UserIndividualPermissions_CurrentActiveGlobal
    ON dbo.User_Individual_Permissions(user_id, permission_code, scope_type, grant_type)
    WHERE assignment_status = 'ACTIVE' AND effective_to IS NULL AND company_id IS NULL;
GO

CREATE TRIGGER dbo.TR_User_Individual_Permissions_PreventOverlap
ON dbo.User_Individual_Permissions
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted AS candidate
        INNER JOIN dbo.User_Individual_Permissions AS existing WITH (UPDLOCK, HOLDLOCK)
            ON existing.user_id = candidate.user_id
            AND existing.permission_code = candidate.permission_code
            AND existing.scope_type = candidate.scope_type
            AND (existing.company_id = candidate.company_id OR (existing.company_id IS NULL AND candidate.company_id IS NULL))
            AND existing.grant_type = candidate.grant_type
            AND existing.id <> candidate.id
            AND candidate.effective_from < COALESCE(existing.effective_to, CONVERT(datetime2(3), '9999-12-31T23:59:59.997'))
            AND existing.effective_from < COALESCE(candidate.effective_to, CONVERT(datetime2(3), '9999-12-31T23:59:59.997'))
    )
    BEGIN
        THROW 51021, 'Individual permission effective periods may not overlap within the same grant stream.', 1;
    END;
END;
GO

CREATE TABLE dbo.Admin_Groups (
    id bigint IDENTITY(1,1) NOT NULL,
    group_code varchar(100) NOT NULL,
    name nvarchar(200) NOT NULL,
    description nvarchar(500) NULL,
    scope_type varchar(30) NOT NULL,
    company_id bigint NULL,
    is_active bit NOT NULL
        CONSTRAINT DF_AdminGroups_IsActive DEFAULT (1),
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_AdminGroups_CreatedAt DEFAULT (SYSUTCDATETIME()),
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_Admin_Groups PRIMARY KEY (id),
    CONSTRAINT UQ_AdminGroups_GroupCode UNIQUE (group_code),
    CONSTRAINT FK_AdminGroups_Company FOREIGN KEY (company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT FK_AdminGroups_CreatedBy FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_AdminGroups_UpdatedBy FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT CK_AdminGroups_ScopeType CHECK (scope_type IN ('GLOBAL', 'COMPANY')),
    CONSTRAINT CK_AdminGroups_ScopeCompany CHECK (
        (scope_type = 'GLOBAL' AND company_id IS NULL)
        OR (scope_type = 'COMPANY' AND company_id IS NOT NULL)
    )
);
GO

CREATE TABLE dbo.Admin_Group_Permissions (
    admin_group_id bigint NOT NULL,
    permission_code varchar(100) NOT NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_AdminGroupPermissions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    created_by_user_id bigint NULL,
    CONSTRAINT PK_Admin_Group_Permissions PRIMARY KEY (admin_group_id, permission_code),
    CONSTRAINT FK_AdminGroupPermissions_Group FOREIGN KEY (admin_group_id) REFERENCES dbo.Admin_Groups(id),
    CONSTRAINT FK_AdminGroupPermissions_Permission FOREIGN KEY (permission_code) REFERENCES dbo.Permissions(permission_code),
    CONSTRAINT FK_AdminGroupPermissions_CreatedBy FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id)
);
GO

CREATE TABLE dbo.User_Admin_Group_Assignments (
    id bigint IDENTITY(1,1) NOT NULL,
    user_id bigint NOT NULL,
    admin_group_id bigint NOT NULL,
    assignment_status varchar(30) NOT NULL
        CONSTRAINT DF_UserAdminGroupAssignments_Status DEFAULT ('ACTIVE'),
    effective_from datetime2(3) NOT NULL
        CONSTRAINT DF_UserAdminGroupAssignments_EffectiveFrom DEFAULT (SYSUTCDATETIME()),
    effective_to datetime2(3) NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_UserAdminGroupAssignments_CreatedAt DEFAULT (SYSUTCDATETIME()),
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_User_Admin_Group_Assignments PRIMARY KEY (id),
    CONSTRAINT FK_UserAdminGroupAssignments_User FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserAdminGroupAssignments_Group FOREIGN KEY (admin_group_id) REFERENCES dbo.Admin_Groups(id),
    CONSTRAINT FK_UserAdminGroupAssignments_CreatedBy FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserAdminGroupAssignments_UpdatedBy FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT CK_UserAdminGroupAssignments_Status CHECK (assignment_status IN ('SCHEDULED', 'ACTIVE', 'CLOSED', 'REVOKED')),
    CONSTRAINT CK_UserAdminGroupAssignments_EffectiveDates CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_UserAdminGroupAssignments_StatusDates CHECK (
        assignment_status IN ('SCHEDULED', 'ACTIVE')
        OR (assignment_status IN ('CLOSED', 'REVOKED') AND effective_to IS NOT NULL)
    )
);
GO

CREATE INDEX IX_UserAdminGroupAssignments_OverlapLookup
    ON dbo.User_Admin_Group_Assignments(user_id, admin_group_id, effective_from, effective_to);
GO

CREATE UNIQUE INDEX UQ_UserAdminGroupAssignments_CurrentActive
    ON dbo.User_Admin_Group_Assignments(user_id, admin_group_id)
    WHERE assignment_status = 'ACTIVE' AND effective_to IS NULL;
GO

CREATE TRIGGER dbo.TR_User_Admin_Group_Assignments_PreventOverlap
ON dbo.User_Admin_Group_Assignments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted AS candidate
        INNER JOIN dbo.User_Admin_Group_Assignments AS existing WITH (UPDLOCK, HOLDLOCK)
            ON existing.user_id = candidate.user_id
            AND existing.admin_group_id = candidate.admin_group_id
            AND existing.id <> candidate.id
            AND candidate.effective_from < COALESCE(existing.effective_to, CONVERT(datetime2(3), '9999-12-31T23:59:59.997'))
            AND existing.effective_from < COALESCE(candidate.effective_to, CONVERT(datetime2(3), '9999-12-31T23:59:59.997'))
    )
    BEGIN
        THROW 51022, 'Admin Group assignment effective periods may not overlap.', 1;
    END;
END;
GO

CREATE TABLE dbo.Authorization_Policy_State (
    id int NOT NULL,
    policy_version bigint NOT NULL
        CONSTRAINT DF_AuthorizationPolicyState_PolicyVersion DEFAULT (1),
    updated_at datetime2(3) NOT NULL
        CONSTRAINT DF_AuthorizationPolicyState_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    updated_by_user_id bigint NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_Authorization_Policy_State PRIMARY KEY (id),
    CONSTRAINT CK_AuthorizationPolicyState_Singleton CHECK (id = 1),
    CONSTRAINT CK_AuthorizationPolicyState_PolicyVersion CHECK (policy_version >= 1),
    CONSTRAINT FK_AuthorizationPolicyState_UpdatedBy FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id)
);
GO

INSERT INTO dbo.Authorization_Policy_State (id, policy_version, updated_at, updated_by_user_id)
VALUES (1, 1, CONVERT(datetime2(3), '1900-01-01T00:00:00.000'), NULL);
GO

CREATE TABLE dbo.Security_Bootstrap_State (
    id int NOT NULL,
    is_bootstrapped bit NOT NULL
        CONSTRAINT DF_SecurityBootstrapState_IsBootstrapped DEFAULT (0),
    bootstrapped_at datetime2(3) NULL,
    bootstrapped_by_user_id bigint NULL,
    row_version rowversion NOT NULL,
    CONSTRAINT PK_Security_Bootstrap_State PRIMARY KEY (id),
    CONSTRAINT CK_SecurityBootstrapState_Singleton CHECK (id = 1),
    CONSTRAINT CK_SecurityBootstrapState_Consistency CHECK (
        (is_bootstrapped = 0 AND bootstrapped_at IS NULL AND bootstrapped_by_user_id IS NULL)
        OR (is_bootstrapped = 1 AND bootstrapped_at IS NOT NULL AND bootstrapped_by_user_id IS NOT NULL)
    ),
    CONSTRAINT FK_SecurityBootstrapState_BootstrappedBy FOREIGN KEY (bootstrapped_by_user_id) REFERENCES dbo.Users(id)
);
GO

INSERT INTO dbo.Security_Bootstrap_State (id, is_bootstrapped, bootstrapped_at, bootstrapped_by_user_id)
VALUES (1, 0, NULL, NULL);
GO

CREATE TABLE dbo.Security_Audit_Events (
    id bigint IDENTITY(1,1) NOT NULL,
    actor_user_id bigint NULL,
    acting_as_user_id bigint NULL,
    target_user_id bigint NULL,
    company_id bigint NULL,
    event_code varchar(100) NOT NULL,
    entity_type varchar(100) NOT NULL,
    entity_id varchar(100) NULL,
    changed_fields nvarchar(max) NULL,
    before_state_json nvarchar(max) NULL,
    after_state_json nvarchar(max) NULL,
    reason nvarchar(1000) NULL,
    correlation_id uniqueidentifier NOT NULL,
    request_metadata nvarchar(max) NULL,
    outcome varchar(50) NOT NULL,
    policy_version bigint NULL,
    created_at datetime2(3) NOT NULL
        CONSTRAINT DF_SecurityAuditEvents_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Security_Audit_Events PRIMARY KEY (id),
    CONSTRAINT FK_SecurityAuditEvents_Actor FOREIGN KEY (actor_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_SecurityAuditEvents_ActingAs FOREIGN KEY (acting_as_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_SecurityAuditEvents_Target FOREIGN KEY (target_user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_SecurityAuditEvents_Company FOREIGN KEY (company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT CK_SecurityAuditEvents_ChangedFieldsJson CHECK (changed_fields IS NULL OR ISJSON(changed_fields) = 1),
    CONSTRAINT CK_SecurityAuditEvents_BeforeStateJson CHECK (before_state_json IS NULL OR ISJSON(before_state_json) = 1),
    CONSTRAINT CK_SecurityAuditEvents_AfterStateJson CHECK (after_state_json IS NULL OR ISJSON(after_state_json) = 1),
    CONSTRAINT CK_SecurityAuditEvents_RequestMetadataJson CHECK (request_metadata IS NULL OR ISJSON(request_metadata) = 1)
);
GO

CREATE INDEX IX_SecurityAuditEvents_CreatedAt
    ON dbo.Security_Audit_Events(created_at DESC, id DESC);
GO

CREATE INDEX IX_SecurityAuditEvents_CorrelationId
    ON dbo.Security_Audit_Events(correlation_id);
GO

CREATE TRIGGER dbo.TR_Security_Audit_Events_AppendOnly
ON dbo.Security_Audit_Events
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51030, 'Security_Audit_Events is append-only; UPDATE and DELETE are prohibited.', 1;
END;
GO

-- This database role is the ordinary runtime boundary for security audit access.
-- DENY ALTER prevents TRUNCATE for this role because SQL Server requires ALTER on
-- the table to truncate it. The DML trigger above does not and cannot intercept
-- TRUNCATE. Members of db_owner and sysadmin remain outside this boundary.
CREATE ROLE PTKD_Security_Audit_Runtime AUTHORIZATION dbo;
GO

GRANT SELECT ON OBJECT::dbo.Security_Audit_Events TO PTKD_Security_Audit_Runtime;
GRANT INSERT ON OBJECT::dbo.Security_Audit_Events TO PTKD_Security_Audit_Runtime;
DENY UPDATE ON OBJECT::dbo.Security_Audit_Events TO PTKD_Security_Audit_Runtime;
DENY DELETE ON OBJECT::dbo.Security_Audit_Events TO PTKD_Security_Audit_Runtime;
DENY ALTER ON OBJECT::dbo.Security_Audit_Events TO PTKD_Security_Audit_Runtime;
GO
