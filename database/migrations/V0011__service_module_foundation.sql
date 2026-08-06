SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- V0011: Service Module Foundation
-- Phase 1B.6-B Service Module Foundation
-- ============================================================

-- 1. Service_Types (catalog with standard pricing)
CREATE TABLE dbo.Service_Types (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    code                        NVARCHAR(50)         NOT NULL,
    name                        NVARCHAR(200)        NOT NULL,
    description                 NVARCHAR(500)        NULL,
    standard_price              DECIMAL(18,2)        NOT NULL,
    standard_price_currency     NVARCHAR(3)          NOT NULL    CONSTRAINT DF_ST_standard_price_currency DEFAULT 'VND',
    cycle_duration_months       INT                  NULL,
    is_active                   BIT                  NOT NULL    CONSTRAINT DF_ST_is_active DEFAULT 1,
    created_at                  datetime2(3)         NOT NULL,
    updated_at                  datetime2(3)         NULL,
    created_by_user_id          BIGINT               NOT NULL,
    row_version                 ROWVERSION           NOT NULL,

    CONSTRAINT PK_Service_Types PRIMARY KEY (id),
    CONSTRAINT UQ_Service_Types_code UNIQUE (code),
    CONSTRAINT FK_Service_Types_created_by_user_id FOREIGN KEY (created_by_user_id)
        REFERENCES dbo.Users (id)
);

CREATE NONCLUSTERED INDEX IX_Service_Types_is_active
    ON dbo.Service_Types (is_active);

-- 2. Service_Price_History (audit trail of standard price changes)
CREATE TABLE dbo.Service_Price_History (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    service_type_id             BIGINT               NOT NULL,
    price                       DECIMAL(18,2)        NOT NULL,
    effective_from              datetime2(3)         NOT NULL,
    effective_to                datetime2(3)         NULL,
    changed_by_user_id          BIGINT               NOT NULL,
    change_reason               NVARCHAR(500)        NOT NULL,
    created_at                  datetime2(3)         NOT NULL,

    CONSTRAINT PK_Service_Price_History PRIMARY KEY (id),
    CONSTRAINT FK_SPH_service_type_id FOREIGN KEY (service_type_id)
        REFERENCES dbo.Service_Types (id),
    CONSTRAINT FK_SPH_changed_by_user_id FOREIGN KEY (changed_by_user_id)
        REFERENCES dbo.Users (id)
);

CREATE NONCLUSTERED INDEX IX_SPH_service_type_id
    ON dbo.Service_Price_History (service_type_id);

CREATE NONCLUSTERED INDEX IX_SPH_service_type_effective
    ON dbo.Service_Price_History (service_type_id, effective_from);

-- 3. Services (core service instance)
CREATE TABLE dbo.Services (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    service_type_id             BIGINT               NOT NULL,
    customer_id                 BIGINT               NOT NULL,
    company_id                  BIGINT               NOT NULL,
    status                      NVARCHAR(30)         NOT NULL,
    applied_price               DECIMAL(18,2)        NOT NULL,
    standard_price_snapshot     DECIMAL(18,2)        NOT NULL,
    is_override_price           BIT                  NOT NULL    CONSTRAINT DF_S_is_override_price DEFAULT 0,
    override_approval_request_id BIGINT              NULL,
    valid_from                  datetime2(3)         NOT NULL,
    valid_to                    datetime2(3)         NULL,
    cycle_number                INT                  NOT NULL    CONSTRAINT DF_S_cycle_number DEFAULT 1,
    previous_service_id         BIGINT               NULL,
    created_by_user_id          BIGINT               NOT NULL,
    created_at                  datetime2(3)         NOT NULL,
    updated_at                  datetime2(3)         NULL,
    row_version                 ROWVERSION           NOT NULL,

    CONSTRAINT PK_Services PRIMARY KEY (id),
    CONSTRAINT FK_Services_service_type_id FOREIGN KEY (service_type_id)
        REFERENCES dbo.Service_Types (id),
    CONSTRAINT FK_Services_customer_id FOREIGN KEY (customer_id)
        REFERENCES dbo.Customers (id),
    CONSTRAINT FK_Services_company_id FOREIGN KEY (company_id)
        REFERENCES dbo.Companies (id),
    CONSTRAINT FK_Services_previous_service_id FOREIGN KEY (previous_service_id)
        REFERENCES dbo.Services (id),
    CONSTRAINT FK_Services_created_by_user_id FOREIGN KEY (created_by_user_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT CK_Services_status CHECK (status IN (
        'ACTIVE', 'EXPIRED', 'CANCELLED', 'PENDING_PRICE_OVERRIDE'))
);

CREATE NONCLUSTERED INDEX IX_Services_customer_company
    ON dbo.Services (customer_id, company_id);

CREATE NONCLUSTERED INDEX IX_Services_company_status
    ON dbo.Services (company_id, status);

CREATE NONCLUSTERED INDEX IX_Services_service_type
    ON dbo.Services (service_type_id);

CREATE NONCLUSTERED INDEX IX_Services_previous
    ON dbo.Services (previous_service_id)
    WHERE previous_service_id IS NOT NULL;

-- 4. Service_History (audit trail for lifecycle events)
CREATE TABLE dbo.Service_History (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    service_id                  BIGINT               NOT NULL,
    action_code                 NVARCHAR(30)         NOT NULL,
    before_data                 NVARCHAR(MAX)        NULL,
    after_data                  NVARCHAR(MAX)        NULL,
    acted_by_user_id            BIGINT               NOT NULL,
    reason                      NVARCHAR(500)        NULL,
    correlation_id              UNIQUEIDENTIFIER     NOT NULL,
    created_at                  datetime2(3)         NOT NULL,

    CONSTRAINT PK_Service_History PRIMARY KEY (id),
    CONSTRAINT FK_SH_service_id FOREIGN KEY (service_id)
        REFERENCES dbo.Services (id),
    CONSTRAINT FK_SH_acted_by_user_id FOREIGN KEY (acted_by_user_id)
        REFERENCES dbo.Users (id)
);

CREATE NONCLUSTERED INDEX IX_SH_service_id
    ON dbo.Service_History (service_id);

CREATE NONCLUSTERED INDEX IX_SH_service_created
    ON dbo.Service_History (service_id, created_at);

-- 5. Seed permissions
INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('SERVICE_VIEW', 'SERVICE', 'VIEW', 'COMPANY', 0, 0, 0, 1, N'View services within assigned company.'),
    ('SERVICE_TYPE_MANAGE', 'SERVICE', 'MANAGE_CATALOG', 'GLOBAL', 1, 0, 0, 1, N'Manage service type catalog.'),
    ('SERVICE_CREATE_STANDARD', 'SERVICE', 'CREATE', 'COMPANY', 0, 0, 0, 1, N'Create a service at standard terms.'),
    ('SERVICE_RENEW_STANDARD', 'SERVICE', 'RENEW', 'COMPANY', 0, 0, 0, 1, N'Renew at standard snapshot price.'),
    ('SERVICE_PRICE_OVERRIDE_REQUEST', 'SERVICE', 'REQUEST_PRICE', 'COMPANY', 1, 0, 0, 1, N'Request non-standard service pricing.'),
    ('SERVICE_PRICE_OVERRIDE_APPROVE', 'SERVICE', 'APPROVE_PRICE', 'COMPANY', 1, 0, 1, 1, N'Approve a non-standard service price.');

-- 6. Seed business process catalog entries
IF NOT EXISTS (SELECT 1 FROM dbo.Business_Process_Catalog WHERE process_code = 'SERVICE_PRICE_OVERRIDE')
BEGIN
    INSERT INTO dbo.Business_Process_Catalog (process_code, process_name, description, is_approval_required, is_active, created_at)
    VALUES ('SERVICE_PRICE_OVERRIDE', N'Yêu cầu giá dịch vụ ngoại lệ', N'Quy trình phê duyệt giá dịch vụ khác giá chuẩn', 1, 1, SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM dbo.Business_Process_Catalog WHERE process_code = 'RENEW_SERVICE_STANDARD')
BEGIN
    INSERT INTO dbo.Business_Process_Catalog (process_code, process_name, description, is_approval_required, is_active, created_at)
    VALUES ('RENEW_SERVICE_STANDARD', N'Gia hạn dịch vụ chuẩn', N'Gia hạn dịch vụ theo giá chuẩn, không cần phê duyệt', 0, 1, SYSUTCDATETIME());
END

COMMIT TRANSACTION;
