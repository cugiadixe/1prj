-- U0005__drop_customer_schema.sql
-- Test-only rollback for V0005. Drops Customer schema tables and deactivates permission seeds.

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF DB_NAME() <> N'PTKD_TEST_PHASE1A2'
        THROW 51200, 'U0005 may run only against the exact database PTKD_TEST_PHASE1A2.', 1;

    IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
        THROW 51201, 'SchemaVersions is missing; V0005 cannot be rolled back.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SchemaVersions
        WHERE Version = N'V0005'
          AND ScriptName = N'V0005__create_customer_schema.sql'
    )
        THROW 51202, 'V0005 is not recorded in SchemaVersions.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SchemaVersions
        WHERE Version LIKE N'V%'
          AND TRY_CONVERT(int, SUBSTRING(Version, 2, 20)) > 5
    )
        THROW 51203, 'A migration later than V0005 is recorded; rollback is prohibited.', 1;

    -- Check no FK references to customer permissions
    IF EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions WHERE permission_code IN ('CUSTOMER_VIEW_BASIC','CUSTOMER_VIEW_SENSITIVE','CUSTOMER_CREATE_FINAL','CUSTOMER_MASTER_UPDATE'))
        THROW 51204, 'Rollback blocked: Admin_Group_Permissions references customer permissions.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Role_Permissions WHERE permission_code IN ('CUSTOMER_VIEW_BASIC','CUSTOMER_VIEW_SENSITIVE','CUSTOMER_CREATE_FINAL','CUSTOMER_MASTER_UPDATE'))
        THROW 51205, 'Rollback blocked: Role_Permissions references customer permissions.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Department_Permissions WHERE permission_code IN ('CUSTOMER_VIEW_BASIC','CUSTOMER_VIEW_SENSITIVE','CUSTOMER_CREATE_FINAL','CUSTOMER_MASTER_UPDATE'))
        THROW 51206, 'Rollback blocked: Department_Permissions references customer permissions.', 1;
    IF EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions WHERE permission_code IN ('CUSTOMER_VIEW_BASIC','CUSTOMER_VIEW_SENSITIVE','CUSTOMER_CREATE_FINAL','CUSTOMER_MASTER_UPDATE'))
        THROW 51207, 'Rollback blocked: User_Individual_Permissions references customer permissions.', 1;

    -- Drop tables in reverse dependency order
    DROP TABLE IF EXISTS dbo.Customer_Company_Contexts;
    DROP TABLE IF EXISTS dbo.Customers;
    DROP TABLE IF EXISTS dbo.Profiles;

    -- Deactivate permission seeds (no DELETE due to TR_Permissions_PreventDelete trigger)
    UPDATE dbo.Permissions
    SET is_active = 0
    WHERE permission_code IN ('CUSTOMER_VIEW_BASIC', 'CUSTOMER_VIEW_SENSITIVE', 'CUSTOMER_CREATE_FINAL', 'CUSTOMER_MASTER_UPDATE');

    DELETE FROM dbo.SchemaVersions
    WHERE Version = N'V0005'
      AND ScriptName = N'V0005__create_customer_schema.sql';

    IF @@ROWCOUNT <> 1
        THROW 51209, 'V0005 SchemaVersions row was not removed exactly once.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
