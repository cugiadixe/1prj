SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Add target fields for CUSTOMER_MASTER_CHANGE

ALTER TABLE dbo.[Customer_Change_Requests]
ADD [target_customer_id] BIGINT NULL,
    [target_row_version] BINARY(8) NULL;
GO

ALTER TABLE dbo.[Customer_Change_Requests]
ADD CONSTRAINT [FK_Customer_Change_Requests_TargetCustomer] FOREIGN KEY ([target_customer_id]) REFERENCES dbo.[Customers]([id]);
GO

CREATE INDEX [IX_CCR_target_customer] ON dbo.[Customer_Change_Requests]([target_customer_id]) WHERE [target_customer_id] IS NOT NULL;
GO

COMMIT TRANSACTION;
GO
