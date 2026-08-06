-- V0005__create_customer_schema.sql
-- Phase 1B.2-B1: Customer module schema — Profiles, Customers, Customer_Company_Contexts.
-- Seed 4 first-slice customer permission codes into dbo.Permissions.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ══════════════════════════════════════════════════════════════════════════
-- 1. Profiles
-- ══════════════════════════════════════════════════════════════════════════

CREATE TABLE dbo.Profiles
(
    id                  bigint          IDENTITY(1,1)   NOT NULL,
    full_name           nvarchar(200)                   NOT NULL,
    cccd                varchar(20)                     NULL,
    dob                 date                            NULL,
    dob_partial         varchar(10)                     NULL,
    dob_precision       varchar(10)                     NULL,
    gender              varchar(10)                     NULL,
    permanent_address   nvarchar(500)                   NULL,
    cccd_issue_date     date                            NULL,
    cccd_issue_place    nvarchar(200)                   NULL,
    tax_code            varchar(20)                     NULL,
    phone               varchar(20)                     NULL,
    contact_address     nvarchar(500)                   NULL,
    death_date_solar    date                            NULL,
    death_date_lunar    varchar(20)                     NULL,
    death_place         nvarchar(200)                   NULL,
    hometown            nvarchar(200)                   NULL,
    is_active           bit                             NOT NULL    CONSTRAINT DF_Profiles_is_active DEFAULT 1,
    created_at          datetime2(3)                    NOT NULL,
    created_by_user_id  bigint                          NULL,
    updated_at          datetime2(3)                    NULL,
    updated_by_user_id  bigint                          NULL,
    row_version         rowversion                      NOT NULL,

    CONSTRAINT PK_Profiles PRIMARY KEY (id),
    CONSTRAINT CK_Profiles_dob_precision CHECK (dob_precision IN ('FULL', 'YEAR_MONTH', 'YEAR', 'UNKNOWN')),
    CONSTRAINT CK_Profiles_gender CHECK (gender IN ('MALE', 'FEMALE', 'OTHER'))
);

CREATE UNIQUE NONCLUSTERED INDEX UQ_Profiles_cccd_active
    ON dbo.Profiles (cccd)
    WHERE cccd IS NOT NULL AND is_active = 1;

-- ══════════════════════════════════════════════════════════════════════════
-- 2. Customers
-- ══════════════════════════════════════════════════════════════════════════

CREATE TABLE dbo.Customers
(
    id                      bigint          IDENTITY(1,1)   NOT NULL,
    customer_code           nvarchar(50)                    NOT NULL,
    profile_id              bigint                          NOT NULL,
    customer_status         varchar(20)                     NOT NULL,
    survivor_customer_id    bigint                          NULL,
    created_at              datetime2(3)                    NOT NULL,
    created_by_user_id      bigint                          NULL,
    updated_at              datetime2(3)                    NULL,
    updated_by_user_id      bigint                          NULL,
    row_version             rowversion                      NOT NULL,

    CONSTRAINT PK_Customers PRIMARY KEY (id),
    CONSTRAINT UQ_Customers_customer_code UNIQUE (customer_code),
    CONSTRAINT FK_Customers_profile_id FOREIGN KEY (profile_id) REFERENCES dbo.Profiles (id),
    CONSTRAINT CK_Customers_customer_status CHECK (customer_status IN ('ACTIVE', 'INACTIVE', 'MERGED')),
    CONSTRAINT CK_Customers_survivor_null CHECK (customer_status <> 'MERGED' OR survivor_customer_id IS NOT NULL)
);

-- Self-referencing FK for merge survivor
ALTER TABLE dbo.Customers
    ADD CONSTRAINT FK_Customers_survivor_customer_id
    FOREIGN KEY (survivor_customer_id) REFERENCES dbo.Customers (id);

-- ══════════════════════════════════════════════════════════════════════════
-- 3. Customer_Company_Contexts
-- ══════════════════════════════════════════════════════════════════════════

CREATE TABLE dbo.Customer_Company_Contexts
(
    id                      bigint          IDENTITY(1,1)   NOT NULL,
    customer_id             bigint                          NOT NULL,
    company_id              bigint                          NOT NULL,
    assigned_staff_id       bigint                          NULL,
    relationship_status     varchar(20)                     NOT NULL,
    internal_notes          nvarchar(2000)                  NULL,
    first_interaction_at    datetime2(3)                    NULL,
    last_interaction_at     datetime2(3)                    NULL,
    created_at              datetime2(3)                    NOT NULL,
    created_by_user_id      bigint                          NULL,
    updated_at              datetime2(3)                    NULL,
    updated_by_user_id      bigint                          NULL,
    row_version             rowversion                      NOT NULL,

    CONSTRAINT PK_Customer_Company_Contexts PRIMARY KEY (id),
    CONSTRAINT UQ_Customer_Company_Contexts_customer_company UNIQUE (customer_id, company_id),
    CONSTRAINT FK_Customer_Company_Contexts_customer_id FOREIGN KEY (customer_id) REFERENCES dbo.Customers (id),
    CONSTRAINT FK_Customer_Company_Contexts_company_id FOREIGN KEY (company_id) REFERENCES dbo.Companies (id),
    CONSTRAINT FK_Customer_Company_Contexts_assigned_staff_id FOREIGN KEY (assigned_staff_id) REFERENCES dbo.Users (id),
    CONSTRAINT CK_Customer_Company_Contexts_relationship_status CHECK (relationship_status IN ('ACTIVE', 'INACTIVE'))
);

-- ══════════════════════════════════════════════════════════════════════════
-- 4. Audit FKs to Users (added after tables exist to avoid ordering issues)
-- ══════════════════════════════════════════════════════════════════════════

ALTER TABLE dbo.Profiles
    ADD CONSTRAINT FK_Profiles_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id);
ALTER TABLE dbo.Profiles
    ADD CONSTRAINT FK_Profiles_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id);

ALTER TABLE dbo.Customers
    ADD CONSTRAINT FK_Customers_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id);
ALTER TABLE dbo.Customers
    ADD CONSTRAINT FK_Customers_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id);

ALTER TABLE dbo.Customer_Company_Contexts
    ADD CONSTRAINT FK_Customer_Company_Contexts_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users (id);
ALTER TABLE dbo.Customer_Company_Contexts
    ADD CONSTRAINT FK_Customer_Company_Contexts_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users (id);

-- ══════════════════════════════════════════════════════════════════════════
-- 5. Seed first-slice customer permission codes
-- ══════════════════════════════════════════════════════════════════════════

INSERT INTO dbo.Permissions
    (permission_code, module_code, action_code, data_scope, is_sensitive, requires_reason, is_delegable, is_active, description)
VALUES
    ('CUSTOMER_VIEW_BASIC', 'CUSTOMER', 'VIEW', 'GLOBAL', 0, 0, 0, 1, N'View customer list and basic details.'),
    ('CUSTOMER_VIEW_SENSITIVE', 'CUSTOMER', 'VIEW_SENSITIVE', 'GLOBAL', 1, 0, 0, 1, N'View unmasked sensitive customer fields (CCCD, address, phone).'),
    ('CUSTOMER_CREATE_FINAL', 'CUSTOMER', 'CREATE_FINAL', 'GLOBAL', 1, 0, 0, 1, N'Directly create a new customer record (admin operation).'),
    ('CUSTOMER_MASTER_UPDATE', 'CUSTOMER', 'UPDATE_MASTER', 'GLOBAL', 1, 1, 0, 1, N'Directly update customer master data (admin operation, requires reason).');

COMMIT TRANSACTION;
