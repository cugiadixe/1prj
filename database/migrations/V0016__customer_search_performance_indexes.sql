SET QUOTED_IDENTIFIER ON;
GO

-- Performance indexes for customer search with 1M+ records.
-- Idempotent: guarded with IF NOT EXISTS so re-runs are safe.

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_profile_id' AND object_id = OBJECT_ID('dbo.Customers'))
    CREATE NONCLUSTERED INDEX IX_Customers_profile_id
    ON dbo.Customers (profile_id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_customer_status' AND object_id = OBJECT_ID('dbo.Customers'))
    CREATE NONCLUSTERED INDEX IX_Customers_customer_status
    ON dbo.Customers (customer_status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_id_profile' AND object_id = OBJECT_ID('dbo.Customers'))
    CREATE NONCLUSTERED INDEX IX_Customers_id_profile
    ON dbo.Customers (id)
    INCLUDE (profile_id, customer_code, customer_status, created_at);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Profiles_full_name' AND object_id = OBJECT_ID('dbo.Profiles'))
    CREATE NONCLUSTERED INDEX IX_Profiles_full_name
    ON dbo.Profiles (full_name)
    INCLUDE (cccd, phone);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_customer_code_search' AND object_id = OBJECT_ID('dbo.Customers'))
    CREATE NONCLUSTERED INDEX IX_Customers_customer_code_search
    ON dbo.Customers (customer_code)
    INCLUDE (profile_id, customer_status);
