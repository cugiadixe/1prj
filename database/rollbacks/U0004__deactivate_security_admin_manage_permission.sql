-- U0004__deactivate_security_admin_manage_permission.sql
-- Test-only, data-protecting rollback for V0004.

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF DB_NAME() <> N'PTKD_TEST_PHASE1A2'
        THROW 51200, 'U0004 may run only against the exact database PTKD_TEST_PHASE1A2.', 1;

    IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
        THROW 51201, 'SchemaVersions is missing; V0004 cannot be rolled back.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SchemaVersions
        WHERE Version = N'V0004'
          AND ScriptName = N'V0004__seed_security_admin_manage_permission.sql'
    )
        THROW 51202, 'V0004 is not recorded in SchemaVersions.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.SchemaVersions
        WHERE Version LIKE N'V%'
          AND TRY_CONVERT(int, SUBSTRING(Version, 2, 20)) > 4
    )
        THROW 51203, 'A migration later than V0004 is recorded; rollback is prohibited.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Admin_Group_Permissions WHERE permission_code = 'SECURITY_ADMIN_MANAGE')
        THROW 51204, 'Rollback blocked: Admin_Group_Permissions references SECURITY_ADMIN_MANAGE.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Role_Permissions WHERE permission_code = 'SECURITY_ADMIN_MANAGE')
        THROW 51205, 'Rollback blocked: Role_Permissions references SECURITY_ADMIN_MANAGE.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Department_Permissions WHERE permission_code = 'SECURITY_ADMIN_MANAGE')
        THROW 51206, 'Rollback blocked: Department_Permissions references SECURITY_ADMIN_MANAGE.', 1;
    IF EXISTS (SELECT 1 FROM dbo.User_Individual_Permissions WHERE permission_code = 'SECURITY_ADMIN_MANAGE')
        THROW 51207, 'Rollback blocked: User_Individual_Permissions references SECURITY_ADMIN_MANAGE.', 1;

    -- Deactivate (no DELETE due to TR_Permissions_PreventDelete trigger)
    UPDATE dbo.Permissions
    SET is_active = 0
    WHERE permission_code = 'SECURITY_ADMIN_MANAGE';

    IF @@ROWCOUNT <> 1
        THROW 51208, 'SECURITY_ADMIN_MANAGE row was not updated exactly once.', 1;

    DELETE FROM dbo.SchemaVersions
    WHERE Version = N'V0004'
      AND ScriptName = N'V0004__seed_security_admin_manage_permission.sql';

    IF @@ROWCOUNT <> 1
        THROW 51209, 'V0004 SchemaVersions row was not removed exactly once.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
