SET XACT_ABORT ON;
BEGIN TRANSACTION;

DROP TABLE IF EXISTS dbo.Care_Package_Request_Items;
DROP TABLE IF EXISTS dbo.Care_Package_Requests;

DISABLE TRIGGER dbo.TR_Permissions_PreventDelete ON dbo.Permissions;
DELETE FROM dbo.Permissions WHERE permission_code IN ('CARE_PACKAGE_VIEW', 'CARE_PACKAGE_CREATE');
ENABLE TRIGGER dbo.TR_Permissions_PreventDelete ON dbo.Permissions;

DELETE FROM dbo.SchemaVersions WHERE ScriptName LIKE '%V0014%';

COMMIT TRANSACTION;
