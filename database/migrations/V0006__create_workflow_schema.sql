SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- V0006: Workflow/Approval Engine Schema
-- Phase 1B.3-B1 Workflow Backend Foundation
-- ============================================================

-- 1. Business_Process_Catalog (DEV-managed, admin read-only)
CREATE TABLE dbo.Business_Process_Catalog (
    process_code        VARCHAR(100)        NOT NULL,
    process_name        NVARCHAR(500)       NOT NULL,
    description         NVARCHAR(2000)      NULL,
    is_approval_required BIT                NOT NULL    CONSTRAINT DF_BPC_is_approval_required DEFAULT 1,
    is_active           BIT                 NOT NULL    CONSTRAINT DF_BPC_is_active DEFAULT 1,
    created_at          datetime2(3)        NOT NULL,
    updated_at          datetime2(3)        NULL,

    CONSTRAINT PK_Business_Process_Catalog PRIMARY KEY (process_code)
);

-- 2. Workflow_Definitions
CREATE TABLE dbo.Workflow_Definitions (
    id                  BIGINT IDENTITY(1,1) NOT NULL,
    definition_code     VARCHAR(100)         NOT NULL,
    definition_name     NVARCHAR(500)        NOT NULL,
    description         NVARCHAR(2000)       NULL,
    process_code        VARCHAR(100)         NOT NULL,
    is_active           BIT                  NOT NULL    CONSTRAINT DF_WD_is_active DEFAULT 1,
    created_by          BIGINT               NOT NULL,
    created_at          datetime2(3)         NOT NULL,
    updated_at          datetime2(3)         NULL,
    row_version         ROWVERSION           NOT NULL,

    CONSTRAINT PK_Workflow_Definitions PRIMARY KEY (id),
    CONSTRAINT UQ_Workflow_Definitions_definition_code UNIQUE (definition_code),
    CONSTRAINT FK_Workflow_Definitions_process_code FOREIGN KEY (process_code)
        REFERENCES dbo.Business_Process_Catalog (process_code),
    CONSTRAINT FK_Workflow_Definitions_created_by FOREIGN KEY (created_by)
        REFERENCES dbo.Users (id)
);

CREATE NONCLUSTERED INDEX IX_Workflow_Definitions_process_code
    ON dbo.Workflow_Definitions (process_code);

-- 3. Workflow_Definition_Versions
CREATE TABLE dbo.Workflow_Definition_Versions (
    id                      BIGINT IDENTITY(1,1) NOT NULL,
    workflow_definition_id  BIGINT               NOT NULL,
    version_number          INT                  NOT NULL,
    version_status          VARCHAR(20)          NOT NULL,
    effective_from          datetime2(3)         NULL,
    effective_to            datetime2(3)         NULL,
    published_at            datetime2(3)         NULL,
    published_by            BIGINT               NULL,
    created_by              BIGINT               NOT NULL,
    created_at              datetime2(3)         NOT NULL,
    updated_at              datetime2(3)         NULL,
    row_version             ROWVERSION           NOT NULL,

    CONSTRAINT PK_Workflow_Definition_Versions PRIMARY KEY (id),
    CONSTRAINT UQ_WDV_definition_version UNIQUE (workflow_definition_id, version_number),
    CONSTRAINT FK_WDV_workflow_definition_id FOREIGN KEY (workflow_definition_id)
        REFERENCES dbo.Workflow_Definitions (id),
    CONSTRAINT FK_WDV_published_by FOREIGN KEY (published_by)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_WDV_created_by FOREIGN KEY (created_by)
        REFERENCES dbo.Users (id),
    CONSTRAINT CK_WDV_version_status CHECK (version_status IN ('DRAFT', 'PUBLISHED', 'ACTIVE', 'RETIRED'))
);

CREATE NONCLUSTERED INDEX IX_WDV_status
    ON dbo.Workflow_Definition_Versions (workflow_definition_id, version_status);

-- 4. Workflow_Steps
CREATE TABLE dbo.Workflow_Steps (
    id                      BIGINT IDENTITY(1,1) NOT NULL,
    workflow_version_id     BIGINT               NOT NULL,
    step_order              INT                  NOT NULL,
    step_name               NVARCHAR(500)        NOT NULL,
    description             NVARCHAR(2000)       NULL,
    is_required             BIT                  NOT NULL    CONSTRAINT DF_WS_is_required DEFAULT 1,
    due_duration_minutes    INT                  NULL,
    reminder_before_minutes INT                  NULL,
    created_at              datetime2(3)         NOT NULL,
    updated_at              datetime2(3)         NULL,
    row_version             ROWVERSION           NOT NULL,

    CONSTRAINT PK_Workflow_Steps PRIMARY KEY (id),
    CONSTRAINT UQ_WS_version_order UNIQUE (workflow_version_id, step_order),
    CONSTRAINT FK_WS_workflow_version_id FOREIGN KEY (workflow_version_id)
        REFERENCES dbo.Workflow_Definition_Versions (id)
);

-- 5. Workflow_Step_Approver_Rules
CREATE TABLE dbo.Workflow_Step_Approver_Rules (
    id                      BIGINT IDENTITY(1,1) NOT NULL,
    workflow_step_id        BIGINT               NOT NULL,
    approver_source_type    VARCHAR(50)          NOT NULL,
    approver_source_value   NVARCHAR(500)        NOT NULL,
    priority                INT                  NOT NULL    CONSTRAINT DF_WSAR_priority DEFAULT 0,
    created_at              datetime2(3)         NOT NULL,

    CONSTRAINT PK_Workflow_Step_Approver_Rules PRIMARY KEY (id),
    CONSTRAINT FK_WSAR_workflow_step_id FOREIGN KEY (workflow_step_id)
        REFERENCES dbo.Workflow_Steps (id),
    CONSTRAINT CK_WSAR_source_type CHECK (approver_source_type IN (
        'SPECIFIC_USER', 'ROLE', 'DEPARTMENT', 'DEPARTMENT_MANAGER',
        'REQUESTER_MANAGER', 'PERMISSION', 'ADMIN_GROUP'))
);

-- 6. Workflow_Conditions
CREATE TABLE dbo.Workflow_Conditions (
    id                      BIGINT IDENTITY(1,1) NOT NULL,
    workflow_version_id     BIGINT               NOT NULL,
    field_code              VARCHAR(100)         NOT NULL,
    operator                VARCHAR(20)          NOT NULL,
    value                   NVARCHAR(1000)       NOT NULL,
    created_at              datetime2(3)         NOT NULL,

    CONSTRAINT PK_Workflow_Conditions PRIMARY KEY (id),
    CONSTRAINT FK_WC_workflow_version_id FOREIGN KEY (workflow_version_id)
        REFERENCES dbo.Workflow_Definition_Versions (id),
    CONSTRAINT CK_WC_operator CHECK (operator IN ('EQ', 'NEQ', 'GT', 'LT', 'GTE', 'LTE', 'IN', 'CONTAINS'))
);

-- 7. Workflow_Bindings
CREATE TABLE dbo.Workflow_Bindings (
    id                      BIGINT IDENTITY(1,1) NOT NULL,
    workflow_version_id     BIGINT               NOT NULL,
    process_code            VARCHAR(100)         NOT NULL,
    scope_type              VARCHAR(20)          NOT NULL,
    company_id              BIGINT               NULL,
    priority                INT                  NOT NULL    CONSTRAINT DF_WB_priority DEFAULT 0,
    effective_from          datetime2(3)         NOT NULL,
    effective_to            datetime2(3)         NULL,
    is_active               BIT                  NOT NULL    CONSTRAINT DF_WB_is_active DEFAULT 1,
    created_by              BIGINT               NOT NULL,
    created_at              datetime2(3)         NOT NULL,
    updated_at              datetime2(3)         NULL,
    row_version             ROWVERSION           NOT NULL,

    CONSTRAINT PK_Workflow_Bindings PRIMARY KEY (id),
    CONSTRAINT FK_WB_workflow_version_id FOREIGN KEY (workflow_version_id)
        REFERENCES dbo.Workflow_Definition_Versions (id),
    CONSTRAINT FK_WB_process_code FOREIGN KEY (process_code)
        REFERENCES dbo.Business_Process_Catalog (process_code),
    CONSTRAINT FK_WB_company_id FOREIGN KEY (company_id)
        REFERENCES dbo.Companies (id),
    CONSTRAINT FK_WB_created_by FOREIGN KEY (created_by)
        REFERENCES dbo.Users (id),
    CONSTRAINT CK_WB_scope_type CHECK (scope_type IN ('GLOBAL', 'COMPANY')),
    CONSTRAINT CK_WB_company_scope CHECK (
        (scope_type = 'GLOBAL' AND company_id IS NULL)
        OR (scope_type = 'COMPANY' AND company_id IS NOT NULL)
    )
);

CREATE NONCLUSTERED INDEX IX_WB_process_scope
    ON dbo.Workflow_Bindings (process_code, scope_type, company_id)
    WHERE is_active = 1;

-- 8. Workflow_Instances
CREATE TABLE dbo.Workflow_Instances (
    id                      BIGINT IDENTITY(1,1) NOT NULL,
    workflow_version_id     BIGINT               NOT NULL,
    workflow_binding_id     BIGINT               NOT NULL,
    process_code            VARCHAR(100)         NOT NULL,
    company_id              BIGINT               NULL,
    requester_id            BIGINT               NOT NULL,
    business_entity_type    VARCHAR(100)         NOT NULL,
    business_entity_id      BIGINT               NOT NULL,
    instance_status         VARCHAR(30)          NOT NULL,
    round_no                INT                  NOT NULL    CONSTRAINT DF_WI_round_no DEFAULT 1,
    workflow_snapshot_json   NVARCHAR(MAX)       NOT NULL,
    payload_json            NVARCHAR(MAX)        NOT NULL,
    payload_hash            VARCHAR(128)         NOT NULL,
    correlation_id          UNIQUEIDENTIFIER     NOT NULL,
    before_data_json        NVARCHAR(MAX)        NULL,
    after_data_json         NVARCHAR(MAX)        NULL,
    created_at              datetime2(3)         NOT NULL,
    updated_at              datetime2(3)         NULL,
    row_version             ROWVERSION           NOT NULL,

    CONSTRAINT PK_Workflow_Instances PRIMARY KEY (id),
    CONSTRAINT FK_WI_workflow_version_id FOREIGN KEY (workflow_version_id)
        REFERENCES dbo.Workflow_Definition_Versions (id),
    CONSTRAINT FK_WI_workflow_binding_id FOREIGN KEY (workflow_binding_id)
        REFERENCES dbo.Workflow_Bindings (id),
    CONSTRAINT FK_WI_requester_id FOREIGN KEY (requester_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT CK_WI_instance_status CHECK (instance_status IN (
        'PENDING_APPROVAL', 'APPROVED', 'RETURNED', 'WITHDRAWN',
        'PENDING_EXECUTION', 'EXECUTING', 'EXECUTED', 'FAILED'))
);

CREATE NONCLUSTERED INDEX IX_WI_requester
    ON dbo.Workflow_Instances (requester_id, instance_status);

CREATE NONCLUSTERED INDEX IX_WI_business_entity
    ON dbo.Workflow_Instances (business_entity_type, business_entity_id);

CREATE NONCLUSTERED INDEX IX_WI_process_company
    ON dbo.Workflow_Instances (process_code, company_id, instance_status);

-- 9. Workflow_Instance_Steps
CREATE TABLE dbo.Workflow_Instance_Steps (
    id                      BIGINT IDENTITY(1,1) NOT NULL,
    workflow_instance_id    BIGINT               NOT NULL,
    workflow_step_id        BIGINT               NOT NULL,
    step_order              INT                  NOT NULL,
    step_name               NVARCHAR(500)        NOT NULL,
    round_no                INT                  NOT NULL,
    step_status             VARCHAR(20)          NOT NULL,
    is_overdue              BIT                  NOT NULL    CONSTRAINT DF_WIS_is_overdue DEFAULT 0,
    assigned_at             datetime2(3)         NULL,
    completed_at            datetime2(3)         NULL,
    completed_by            BIGINT               NULL,
    created_at              datetime2(3)         NOT NULL,
    updated_at              datetime2(3)         NULL,
    row_version             ROWVERSION           NOT NULL,

    CONSTRAINT PK_Workflow_Instance_Steps PRIMARY KEY (id),
    CONSTRAINT FK_WIS_workflow_instance_id FOREIGN KEY (workflow_instance_id)
        REFERENCES dbo.Workflow_Instances (id),
    CONSTRAINT FK_WIS_workflow_step_id FOREIGN KEY (workflow_step_id)
        REFERENCES dbo.Workflow_Steps (id),
    CONSTRAINT FK_WIS_completed_by FOREIGN KEY (completed_by)
        REFERENCES dbo.Users (id),
    CONSTRAINT CK_WIS_step_status CHECK (step_status IN ('WAITING', 'PENDING', 'APPROVED', 'RETURNED', 'CANCELLED'))
);

CREATE NONCLUSTERED INDEX IX_WIS_instance_round
    ON dbo.Workflow_Instance_Steps (workflow_instance_id, round_no, step_order);

-- 10. Workflow_Instance_Step_Assignees
CREATE TABLE dbo.Workflow_Instance_Step_Assignees (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    workflow_instance_step_id   BIGINT               NOT NULL,
    user_id                     BIGINT               NOT NULL,
    approver_source_type        VARCHAR(50)          NOT NULL,
    is_resolved                 BIT                  NOT NULL    CONSTRAINT DF_WISA_is_resolved DEFAULT 1,
    created_at                  datetime2(3)         NOT NULL,

    CONSTRAINT PK_Workflow_Instance_Step_Assignees PRIMARY KEY (id),
    CONSTRAINT UQ_WISA_step_user UNIQUE (workflow_instance_step_id, user_id),
    CONSTRAINT FK_WISA_workflow_instance_step_id FOREIGN KEY (workflow_instance_step_id)
        REFERENCES dbo.Workflow_Instance_Steps (id),
    CONSTRAINT FK_WISA_user_id FOREIGN KEY (user_id)
        REFERENCES dbo.Users (id)
);

-- 11. Workflow_Actions (append-only)
CREATE TABLE dbo.Workflow_Actions (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    workflow_instance_step_id   BIGINT               NOT NULL,
    workflow_instance_id        BIGINT               NOT NULL,
    action_type                 VARCHAR(20)          NOT NULL,
    acted_by                    BIGINT               NOT NULL,
    on_behalf_of                BIGINT               NULL,
    delegation_id               BIGINT               NULL,
    reason                      NVARCHAR(2000)       NULL,
    comment                     NVARCHAR(4000)       NULL,
    correlation_id              UNIQUEIDENTIFIER     NOT NULL,
    created_at                  datetime2(3)         NOT NULL,

    CONSTRAINT PK_Workflow_Actions PRIMARY KEY (id),
    CONSTRAINT FK_WA_workflow_instance_step_id FOREIGN KEY (workflow_instance_step_id)
        REFERENCES dbo.Workflow_Instance_Steps (id),
    CONSTRAINT FK_WA_workflow_instance_id FOREIGN KEY (workflow_instance_id)
        REFERENCES dbo.Workflow_Instances (id),
    CONSTRAINT FK_WA_acted_by FOREIGN KEY (acted_by)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_WA_on_behalf_of FOREIGN KEY (on_behalf_of)
        REFERENCES dbo.Users (id),
    CONSTRAINT CK_WA_action_type CHECK (action_type IN ('APPROVE', 'RETURN', 'REASSIGN'))
);

CREATE NONCLUSTERED INDEX IX_WA_instance_step
    ON dbo.Workflow_Actions (workflow_instance_step_id);

CREATE NONCLUSTERED INDEX IX_WA_acted_by
    ON dbo.Workflow_Actions (acted_by, created_at DESC);

-- ============================================================
-- Seed workflow permissions into dbo.Permissions
-- ============================================================

INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('WORKFLOW_VIEW',             'WORKFLOW', 'VIEW',             'GLOBAL',  0, 0, 0, 1, N'View workflow definitions, versions, bindings, and instance status.'),
    ('WORKFLOW_CONFIG_MANAGE',    'WORKFLOW', 'CONFIG_MANAGE',    'GLOBAL',  1, 1, 0, 1, N'Create and edit DRAFT workflow configuration.'),
    ('WORKFLOW_PUBLISH',          'WORKFLOW', 'PUBLISH',          'GLOBAL',  1, 1, 0, 1, N'Publish, activate, and retire workflow versions.'),
    ('WORKFLOW_BIND_PROCESS',     'WORKFLOW', 'BIND_PROCESS',     'GLOBAL',  1, 1, 0, 1, N'Create and manage workflow bindings to business processes.'),
    ('WORKFLOW_REASSIGN_PENDING', 'WORKFLOW', 'REASSIGN_PENDING', 'COMPANY', 1, 1, 0, 1, N'Reassign a pending approval step to a different approver.'),
    ('WORKFLOW_AUDIT_VIEW',       'WORKFLOW', 'AUDIT_VIEW',       'GLOBAL',  1, 0, 0, 1, N'View workflow configuration and runtime audit logs.');

-- ============================================================
-- Seed initial business process catalog entries
-- ============================================================

INSERT INTO dbo.Business_Process_Catalog (process_code, process_name, description, is_approval_required, is_active, created_at)
VALUES
    ('CREATE_CUSTOMER', N'Tạo khách hàng mới', N'Quy trình tạo khách hàng mới yêu cầu phê duyệt', 1, 1, SYSUTCDATETIME()),
    ('CUSTOMER_MASTER_CHANGE', N'Thay đổi thông tin khách hàng', N'Quy trình thay đổi thông tin chính của khách hàng', 1, 1, SYSUTCDATETIME());

COMMIT TRANSACTION;
GO
