SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- V0007: Customer_Change_Requests table
-- Phase 1B.3-B4 CREATE_CUSTOMER Workflow Pilot
-- ============================================================

CREATE TABLE dbo.Customer_Change_Requests (
    id                      BIGINT IDENTITY(1,1) NOT NULL,
    process_code            VARCHAR(100)         NOT NULL,
    requester_id            BIGINT               NOT NULL,
    company_id              BIGINT               NULL,
    request_status          VARCHAR(30)          NOT NULL,
    payload_json            NVARCHAR(MAX)        NOT NULL,
    workflow_instance_id    BIGINT               NULL,
    created_customer_id     BIGINT               NULL,
    created_at              datetime2(3)         NOT NULL,
    updated_at              datetime2(3)         NULL,
    row_version             ROWVERSION           NOT NULL,

    CONSTRAINT PK_Customer_Change_Requests PRIMARY KEY (id),
    CONSTRAINT FK_CCR_requester_id FOREIGN KEY (requester_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_CCR_company_id FOREIGN KEY (company_id)
        REFERENCES dbo.Companies (id),
    CONSTRAINT FK_CCR_workflow_instance_id FOREIGN KEY (workflow_instance_id)
        REFERENCES dbo.Workflow_Instances (id),
    CONSTRAINT FK_CCR_created_customer_id FOREIGN KEY (created_customer_id)
        REFERENCES dbo.Customers (id),
    CONSTRAINT CK_CCR_request_status CHECK (request_status IN (
        'DRAFT', 'SUBMITTED', 'APPROVED', 'EXECUTED', 'FAILED', 'WITHDRAWN'))
);

CREATE NONCLUSTERED INDEX IX_CCR_requester
    ON dbo.Customer_Change_Requests (requester_id, request_status);

CREATE NONCLUSTERED INDEX IX_CCR_workflow_instance
    ON dbo.Customer_Change_Requests (workflow_instance_id)
    WHERE workflow_instance_id IS NOT NULL;

COMMIT TRANSACTION;
