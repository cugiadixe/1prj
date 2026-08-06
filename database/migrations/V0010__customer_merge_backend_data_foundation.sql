SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- V0010: Customer Merge Backend/Data Foundation
-- Phase 1B.5-B Customer Merge
-- ============================================================

CREATE TABLE dbo.Customer_Merge_Requests (
    id                          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    source_customer_id          BIGINT           NOT NULL,
    target_customer_id          BIGINT           NOT NULL,
    requester_id                BIGINT           NOT NULL,
    request_status              VARCHAR(30)      NOT NULL,
    survivorship_payload        NVARCHAR(MAX)    NOT NULL,
    source_rowversion_snapshot  VARBINARY(8)     NOT NULL,
    target_rowversion_snapshot  VARBINARY(8)     NOT NULL,
    workflow_instance_id        BIGINT           NULL,
    created_at                  datetime2(3)     NOT NULL,
    updated_at                  datetime2(3)     NULL,
    row_version                 ROWVERSION       NOT NULL,

    CONSTRAINT PK_Customer_Merge_Requests PRIMARY KEY (id),
    CONSTRAINT FK_CMR_source_customer_id FOREIGN KEY (source_customer_id)
        REFERENCES dbo.Customers (id),
    CONSTRAINT FK_CMR_target_customer_id FOREIGN KEY (target_customer_id)
        REFERENCES dbo.Customers (id),
    CONSTRAINT FK_CMR_requester_id FOREIGN KEY (requester_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_CMR_workflow_instance_id FOREIGN KEY (workflow_instance_id)
        REFERENCES dbo.Workflow_Instances (id),
    CONSTRAINT CK_CMR_request_status CHECK (request_status IN (
        'DRAFT', 'SUBMITTED', 'APPROVED', 'EXECUTED', 'REJECTED', 'WITHDRAWN')),
    CONSTRAINT CK_CMR_source_target_diff CHECK (source_customer_id <> target_customer_id)
);

CREATE NONCLUSTERED INDEX IX_CMR_source_customer
    ON dbo.Customer_Merge_Requests (source_customer_id);

CREATE NONCLUSTERED INDEX IX_CMR_target_customer
    ON dbo.Customer_Merge_Requests (target_customer_id);

-- Candidates
CREATE TABLE dbo.Customer_Merge_Request_Candidates (
    id                          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    merge_request_id            UNIQUEIDENTIFIER NOT NULL,
    candidate_customer_id       BIGINT           NOT NULL,
    match_type                  VARCHAR(50)      NOT NULL,
    match_confidence            DECIMAL(5,2)     NULL,
    snapshot_payload            NVARCHAR(MAX)    NULL,
    created_at                  datetime2(3)     NOT NULL,

    CONSTRAINT PK_Customer_Merge_Request_Candidates PRIMARY KEY (id),
    CONSTRAINT FK_CMRC_merge_request_id FOREIGN KEY (merge_request_id)
        REFERENCES dbo.Customer_Merge_Requests (id),
    CONSTRAINT FK_CMRC_candidate_customer_id FOREIGN KEY (candidate_customer_id)
        REFERENCES dbo.Customers (id)
);

CREATE NONCLUSTERED INDEX IX_CMRC_merge_request
    ON dbo.Customer_Merge_Request_Candidates (merge_request_id);

-- Audit/History
CREATE TABLE dbo.Customer_Merge_History (
    id                          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    merge_request_id            UNIQUEIDENTIFIER NULL,
    source_customer_id          BIGINT           NOT NULL,
    target_customer_id          BIGINT           NOT NULL,
    action_type                 VARCHAR(50)      NOT NULL,
    actor_id                    BIGINT           NOT NULL,
    summary_payload             NVARCHAR(MAX)    NOT NULL,
    created_at                  datetime2(3)     NOT NULL,

    CONSTRAINT PK_Customer_Merge_History PRIMARY KEY (id),
    CONSTRAINT FK_CMH_source_customer_id FOREIGN KEY (source_customer_id)
        REFERENCES dbo.Customers (id),
    CONSTRAINT FK_CMH_target_customer_id FOREIGN KEY (target_customer_id)
        REFERENCES dbo.Customers (id),
    CONSTRAINT FK_CMH_actor_id FOREIGN KEY (actor_id)
        REFERENCES dbo.Users (id)
);

-- Seed permissions
INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('CUSTOMER_MERGE_REQUEST_CREATE', 'CUSTOMER', 'MERGE_CREATE', 'GLOBAL', 1, 0, 0, 1, N'Create customer merge requests.'),
    ('CUSTOMER_MERGE_REQUEST_VIEW', 'CUSTOMER', 'MERGE_VIEW', 'GLOBAL', 1, 0, 0, 1, N'View customer merge requests.'),
    ('CUSTOMER_MERGE_REQUEST_ADMIN_VIEW', 'CUSTOMER', 'MERGE_ADMIN_VIEW', 'GLOBAL', 1, 0, 0, 1, N'View all customer merge requests.'),
    ('CUSTOMER_MERGE_EXECUTE', 'CUSTOMER', 'MERGE_EXECUTE', 'GLOBAL', 1, 1, 0, 1, N'Execute customer merge requests.');

COMMIT TRANSACTION;
