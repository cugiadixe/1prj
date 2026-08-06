SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.Cards (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    company_id                  BIGINT               NOT NULL,
    grave_id                    NVARCHAR(100)        NULL, 
    service_id                  BIGINT               NULL,
    print_count                 INT                  NOT NULL CONSTRAINT DF_Cards_print_count DEFAULT 0,
    status                      NVARCHAR(50)         NOT NULL CONSTRAINT DF_Cards_status DEFAULT 'ACTIVE',
    created_at                  datetime2(3)         NOT NULL,
    updated_at                  datetime2(3)         NULL,
    created_by_user_id          BIGINT               NOT NULL,
    row_version                 ROWVERSION           NOT NULL,

    CONSTRAINT PK_Cards PRIMARY KEY (id),
    CONSTRAINT FK_Cards_company_id FOREIGN KEY (company_id)
        REFERENCES dbo.Companies (id),
    CONSTRAINT FK_Cards_service_id FOREIGN KEY (service_id)
        REFERENCES dbo.Services (id),
    CONSTRAINT FK_Cards_created_by_user_id FOREIGN KEY (created_by_user_id)
        REFERENCES dbo.Users (id)
);

CREATE NONCLUSTERED INDEX IX_Cards_company_id ON dbo.Cards (company_id);
CREATE NONCLUSTERED INDEX IX_Cards_grave_id ON dbo.Cards (grave_id);

CREATE TABLE dbo.Card_Reprint_Requests (
    id                          BIGINT IDENTITY(1,1) NOT NULL,
    company_id                  BIGINT               NOT NULL,
    card_id                     BIGINT               NOT NULL,
    requester_id                BIGINT               NOT NULL,
    request_type                NVARCHAR(50)         NOT NULL CONSTRAINT DF_CRR_request_type DEFAULT 'REPRINT',
    reprint_number              INT                  NOT NULL,
    fee_amount                  DECIMAL(18,2)        NULL,
    fee_currency                NVARCHAR(3)          NULL,
    reason_code                 NVARCHAR(100)        NULL,
    workflow_instance_id        BIGINT               NULL,
    payment_transaction_id      BIGINT               NULL,
    service_item_id             BIGINT               NULL,
    status                      NVARCHAR(50)         NOT NULL,
    notes                       NVARCHAR(500)        NULL,
    printed_at                  datetime2(3)         NULL,
    printed_by_user_id          BIGINT               NULL,
    released_at                 datetime2(3)         NULL,
    released_by_user_id         BIGINT               NULL,
    created_at                  datetime2(3)         NOT NULL,
    created_by_user_id          BIGINT               NOT NULL,
    updated_at                  datetime2(3)         NULL,
    updated_by_user_id          BIGINT               NULL,
    row_version                 ROWVERSION           NOT NULL,

    CONSTRAINT PK_Card_Reprint_Requests PRIMARY KEY (id),
    CONSTRAINT FK_CRR_company_id FOREIGN KEY (company_id)
        REFERENCES dbo.Companies (id),
    CONSTRAINT FK_CRR_card_id FOREIGN KEY (card_id)
        REFERENCES dbo.Cards (id),
    CONSTRAINT FK_CRR_requester_id FOREIGN KEY (requester_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_CRR_workflow_instance_id FOREIGN KEY (workflow_instance_id)
        REFERENCES dbo.Workflow_Instances (id),
    CONSTRAINT FK_CRR_payment_transaction_id FOREIGN KEY (payment_transaction_id)
        REFERENCES dbo.Payment_Transactions (id),
    CONSTRAINT FK_CRR_service_item_id FOREIGN KEY (service_item_id)
        REFERENCES dbo.Services (id),
    CONSTRAINT FK_CRR_created_by_user_id FOREIGN KEY (created_by_user_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_CRR_updated_by_user_id FOREIGN KEY (updated_by_user_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_CRR_printed_by_user_id FOREIGN KEY (printed_by_user_id)
        REFERENCES dbo.Users (id),
    CONSTRAINT FK_CRR_released_by_user_id FOREIGN KEY (released_by_user_id)
        REFERENCES dbo.Users (id)
);

CREATE NONCLUSTERED INDEX IX_CRR_company_id ON dbo.Card_Reprint_Requests (company_id);
CREATE NONCLUSTERED INDEX IX_CRR_card_id ON dbo.Card_Reprint_Requests (card_id);
CREATE NONCLUSTERED INDEX IX_CRR_workflow_instance_id ON dbo.Card_Reprint_Requests (workflow_instance_id);
CREATE NONCLUSTERED INDEX IX_CRR_payment_transaction_id ON dbo.Card_Reprint_Requests (payment_transaction_id);

COMMIT TRANSACTION;
