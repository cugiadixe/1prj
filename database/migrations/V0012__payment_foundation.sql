SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- V0012: Payment Foundation
-- Phase 1B.7-B Payment Backend/Data Foundation
-- ============================================================

-- 1. Payment_Transactions
CREATE TABLE dbo.Payment_Transactions (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    bill_code                   NVARCHAR(50)         NOT NULL,
    company_id                  BIGINT               NOT NULL,
    customer_id                 BIGINT               NOT NULL,
    payment_method              NVARCHAR(20)         NOT NULL,
    payment_date                DATETIME2(3)         NOT NULL,
    total_amount                DECIMAL(18,2)        NOT NULL,
    currency_code               NVARCHAR(3)          NOT NULL    CONSTRAINT DF_PT_currency_code DEFAULT 'VND',
    status                      NVARCHAR(20)         NOT NULL,
    notes                       NVARCHAR(500)        NULL,
    confirmed_at                DATETIME2(3)         NULL,
    confirmed_by_user_id        BIGINT               NULL,
    created_by_user_id          BIGINT               NOT NULL,
    created_at                  DATETIME2(3)         NOT NULL,
    updated_at                  DATETIME2(3)         NULL,
    is_deleted                  BIT                  NOT NULL    CONSTRAINT DF_PT_is_deleted DEFAULT 0,
    row_version                 ROWVERSION           NOT NULL,

    CONSTRAINT PK_Payment_Transactions PRIMARY KEY (id),
    CONSTRAINT UQ_Payment_Transactions_bill_code UNIQUE (company_id, bill_code),
    CONSTRAINT FK_PT_company_id FOREIGN KEY (company_id)
        REFERENCES dbo.Companies (id),
    CONSTRAINT FK_PT_customer_id FOREIGN KEY (customer_id)
        REFERENCES dbo.Customers (id),
    CONSTRAINT FK_PT_confirmed_by_user_id FOREIGN KEY (confirmed_by_user_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_PT_created_by_user_id FOREIGN KEY (created_by_user_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT CK_PT_status CHECK (status IN ('DRAFT', 'CONFIRMED')),
    CONSTRAINT CK_PT_currency CHECK (currency_code = 'VND')
);

CREATE NONCLUSTERED INDEX IX_PT_company_id
    ON dbo.Payment_Transactions (company_id);

CREATE NONCLUSTERED INDEX IX_PT_customer_id
    ON dbo.Payment_Transactions (customer_id);

CREATE NONCLUSTERED INDEX IX_PT_company_status
    ON dbo.Payment_Transactions (company_id, status);

CREATE NONCLUSTERED INDEX IX_PT_company_payment_date
    ON dbo.Payment_Transactions (company_id, payment_date);

-- 2. Payment_Transaction_Items
CREATE TABLE dbo.Payment_Transaction_Items (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    payment_transaction_id      BIGINT               NOT NULL,
    service_id                  BIGINT               NOT NULL,
    service_type_code           NVARCHAR(50)         NOT NULL,
    service_cycle_number        INT                  NOT NULL,
    amount                      DECIMAL(18,2)        NOT NULL,
    description                 NVARCHAR(500)        NULL,
    created_at                  DATETIME2(3)         NOT NULL,

    CONSTRAINT PK_Payment_Transaction_Items PRIMARY KEY (id),
    CONSTRAINT FK_PTI_payment_transaction_id FOREIGN KEY (payment_transaction_id)
        REFERENCES dbo.Payment_Transactions (id),
    CONSTRAINT FK_PTI_service_id FOREIGN KEY (service_id)
        REFERENCES dbo.Services (id)
);

CREATE NONCLUSTERED INDEX IX_PTI_payment_transaction_id
    ON dbo.Payment_Transaction_Items (payment_transaction_id);

CREATE NONCLUSTERED INDEX IX_PTI_service_id
    ON dbo.Payment_Transaction_Items (service_id);

-- 3. Payment_Correction_History
CREATE TABLE dbo.Payment_Correction_History (
    id                              BIGINT IDENTITY(1,1) NOT NULL,
    payment_transaction_id          BIGINT               NOT NULL,
    corrected_by_user_id            BIGINT               NOT NULL,
    reason                          NVARCHAR(1000)       NOT NULL,
    before_data                     NVARCHAR(MAX)        NOT NULL,
    after_data                      NVARCHAR(MAX)        NOT NULL,
    corrected_fields                NVARCHAR(500)        NOT NULL,
    correlation_id                  UNIQUEIDENTIFIER     NOT NULL,
    affected_reconciliation_periods NVARCHAR(MAX)        NULL,
    created_at                      DATETIME2(3)         NOT NULL,

    CONSTRAINT PK_Payment_Correction_History PRIMARY KEY (id),
    CONSTRAINT FK_PCH_payment_transaction_id FOREIGN KEY (payment_transaction_id)
        REFERENCES dbo.Payment_Transactions (id),
    CONSTRAINT FK_PCH_corrected_by_user_id FOREIGN KEY (corrected_by_user_id)
        REFERENCES dbo.Users (id)
);

CREATE NONCLUSTERED INDEX IX_PCH_payment_transaction_id
    ON dbo.Payment_Correction_History (payment_transaction_id);

CREATE NONCLUSTERED INDEX IX_PCH_created_at
    ON dbo.Payment_Correction_History (created_at);

-- 4. Reconciliation_Periods
CREATE TABLE dbo.Reconciliation_Periods (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    company_id                  BIGINT               NOT NULL,
    period_type                 NVARCHAR(10)         NOT NULL,
    period_date                 DATE                 NOT NULL,
    status                      NVARCHAR(20)         NOT NULL,
    total_amount                DECIMAL(18,2)        NOT NULL    CONSTRAINT DF_RP_total_amount DEFAULT 0,
    transaction_count           INT                  NOT NULL    CONSTRAINT DF_RP_transaction_count DEFAULT 0,
    prepared_by_user_id         BIGINT               NULL,
    prepared_at                 DATETIME2(3)         NULL,
    confirmed_by_user_id        BIGINT               NULL,
    confirmed_at                DATETIME2(3)         NULL,
    notes                       NVARCHAR(500)        NULL,
    created_at                  DATETIME2(3)         NOT NULL,
    updated_at                  DATETIME2(3)         NULL,
    row_version                 ROWVERSION           NOT NULL,

    CONSTRAINT PK_Reconciliation_Periods PRIMARY KEY (id),
    CONSTRAINT UQ_RP_company_period_type_date UNIQUE (company_id, period_type, period_date),
    CONSTRAINT FK_RP_company_id FOREIGN KEY (company_id)
        REFERENCES dbo.Companies (id),
    CONSTRAINT FK_RP_prepared_by_user_id FOREIGN KEY (prepared_by_user_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_RP_confirmed_by_user_id FOREIGN KEY (confirmed_by_user_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT CK_RP_period_type CHECK (period_type IN ('DAILY', 'MONTHLY')),
    CONSTRAINT CK_RP_status CHECK (status IN ('OPEN', 'DIRTY', 'PREPARED', 'CONFIRMED'))
);

CREATE NONCLUSTERED INDEX IX_RP_status
    ON dbo.Reconciliation_Periods (status);

-- 5. Seed permissions
INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('PAYMENT_CREATE_DRAFT', 'PAYMENT', 'CREATE_DRAFT', 'COMPANY', 1, 0, 0, 1, N'Create draft payment/bill.'),
    ('PAYMENT_CONFIRM', 'PAYMENT', 'CONFIRM', 'COMPANY', 1, 0, 0, 1, N'Confirm a valid draft payment.'),
    ('PAYMENT_PRINT', 'PAYMENT', 'PRINT', 'COMPANY', 1, 0, 0, 1, N'Print a confirmed payment/bill.'),
    ('PAYMENT_CORRECT_CONFIRMED', 'PAYMENT', 'CORRECT', 'COMPANY', 1, 0, 0, 1, N'Correct a confirmed payment under hard invariants.'),
    ('RECONCILIATION_PREPARE', 'RECONCILIATION', 'PREPARE', 'COMPANY', 1, 0, 0, 1, N'Prepare reconciliation periods/data.'),
    ('RECONCILIATION_CONFIRM', 'RECONCILIATION', 'CONFIRM', 'COMPANY', 1, 0, 0, 1, N'Confirm reconciliation.');

COMMIT TRANSACTION;
