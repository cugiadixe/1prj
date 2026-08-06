SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.Care_Package_Requests (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    company_id                  BIGINT               NOT NULL,
    customer_id                 BIGINT               NOT NULL,
    status                      NVARCHAR(50)         NOT NULL,
    requires_approval           BIT                  NOT NULL CONSTRAINT DF_CPR_requires_approval DEFAULT 0,
    workflow_instance_id        BIGINT               NULL,
    service_id                  BIGINT               NULL,
    sale_date                   datetime2(3)         NOT NULL,
    subtotal_amount             DECIMAL(18,2)        NOT NULL,
    discount_amount             DECIMAL(18,2)        NOT NULL CONSTRAINT DF_CPR_discount_amount DEFAULT 0,
    discount_reason             NVARCHAR(500)        NULL,
    total_amount                DECIMAL(18,2)        NOT NULL,
    payment_transaction_id      BIGINT               NULL,
    previous_request_id         BIGINT               NULL,
    created_at                  datetime2(3)         NOT NULL,
    created_by_user_id          BIGINT               NOT NULL,
    updated_at                  datetime2(3)         NULL,
    updated_by_user_id          BIGINT               NULL,
    row_version                 ROWVERSION           NOT NULL,

    CONSTRAINT PK_Care_Package_Requests PRIMARY KEY (id),
    CONSTRAINT FK_CPR_company_id FOREIGN KEY (company_id)
        REFERENCES dbo.Companies (id),
    CONSTRAINT FK_CPR_customer_id FOREIGN KEY (customer_id)
        REFERENCES dbo.Customers (id),
    CONSTRAINT FK_CPR_workflow_instance_id FOREIGN KEY (workflow_instance_id)
        REFERENCES dbo.Workflow_Instances (id),
    CONSTRAINT FK_CPR_service_id FOREIGN KEY (service_id)
        REFERENCES dbo.Services (id),
    CONSTRAINT FK_CPR_payment_transaction_id FOREIGN KEY (payment_transaction_id)
        REFERENCES dbo.Payment_Transactions (id),
    CONSTRAINT FK_CPR_previous_request_id FOREIGN KEY (previous_request_id)
        REFERENCES dbo.Care_Package_Requests (id),
    CONSTRAINT FK_CPR_created_by_user_id FOREIGN KEY (created_by_user_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_CPR_updated_by_user_id FOREIGN KEY (updated_by_user_id)
        REFERENCES dbo.Users (id)
);

CREATE NONCLUSTERED INDEX IX_CPR_company_id ON dbo.Care_Package_Requests (company_id);
CREATE NONCLUSTERED INDEX IX_CPR_customer_id ON dbo.Care_Package_Requests (customer_id);
CREATE NONCLUSTERED INDEX IX_CPR_workflow_instance_id ON dbo.Care_Package_Requests (workflow_instance_id);

CREATE TABLE dbo.Care_Package_Request_Items (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    care_package_request_id     BIGINT               NOT NULL,
    grave_id                    NVARCHAR(100)        NULL, 
    cot_count_snapshot          INT                  NOT NULL,
    service_period_start_date   datetime2(3)         NOT NULL,
    service_period_end_date     datetime2(3)         NOT NULL,
    unit_price_snapshot         DECIMAL(18,2)        NOT NULL,
    line_subtotal               DECIMAL(18,2)        NOT NULL,
    notes                       NVARCHAR(500)        NULL,
    created_at                  datetime2(3)         NOT NULL,
    updated_at                  datetime2(3)         NULL,

    CONSTRAINT PK_Care_Package_Request_Items PRIMARY KEY (id),
    CONSTRAINT FK_CPRI_request_id FOREIGN KEY (care_package_request_id)
        REFERENCES dbo.Care_Package_Requests (id)
);

CREATE NONCLUSTERED INDEX IX_CPRI_request_id ON dbo.Care_Package_Request_Items (care_package_request_id);

INSERT INTO dbo.Permissions (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, description)
VALUES 
    ('CARE_PACKAGE_VIEW', 'SALES', 'VIEW', 'COMPANY', 0, 0, 0, 'View Care Packages'),
    ('CARE_PACKAGE_CREATE', 'SALES', 'CREATE', 'COMPANY', 0, 0, 1, 'Create Care Packages');

COMMIT TRANSACTION;
