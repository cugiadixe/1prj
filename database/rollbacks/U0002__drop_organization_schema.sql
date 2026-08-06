SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    -- Precondition 1: V0002 must be recorded
    IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE Version = 'V0002' AND ScriptName LIKE 'V0002__%')
    BEGIN
        RAISERROR('V0002 is not recorded in SchemaVersions. Cannot rollback.', 16, 1);
    END

    -- Precondition 2: No later numeric migration exists
    -- Ensure we only parse versions starting with 'V' safely
    IF EXISTS (
        SELECT 1 FROM dbo.SchemaVersions 
        WHERE Version LIKE 'V%' 
          AND TRY_CAST(SUBSTRING(Version, 2, LEN(Version)) AS INT) > 2
    )
    BEGIN
        RAISERROR('A migration later than V0002 exists. Cannot rollback V0002.', 16, 1);
    END

    -- Drop Foreign Keys that create circular dependencies (created_by/updated_by)
    IF OBJECT_ID('dbo.FK_EmploymentHistories_created_by', 'F') IS NOT NULL
        ALTER TABLE dbo.Employment_Histories DROP CONSTRAINT FK_EmploymentHistories_created_by;
        
    IF OBJECT_ID('dbo.FK_UserDepartmentAssignments_created_by', 'F') IS NOT NULL
        ALTER TABLE dbo.User_Department_Assignments DROP CONSTRAINT FK_UserDepartmentAssignments_created_by;
    IF OBJECT_ID('dbo.FK_UserDepartmentAssignments_updated_by', 'F') IS NOT NULL
        ALTER TABLE dbo.User_Department_Assignments DROP CONSTRAINT FK_UserDepartmentAssignments_updated_by;

    IF OBJECT_ID('dbo.FK_UserCompanyAssignments_created_by', 'F') IS NOT NULL
        ALTER TABLE dbo.User_Company_Assignments DROP CONSTRAINT FK_UserCompanyAssignments_created_by;
    IF OBJECT_ID('dbo.FK_UserCompanyAssignments_updated_by', 'F') IS NOT NULL
        ALTER TABLE dbo.User_Company_Assignments DROP CONSTRAINT FK_UserCompanyAssignments_updated_by;

    IF OBJECT_ID('dbo.FK_Departments_created_by', 'F') IS NOT NULL
        ALTER TABLE dbo.Departments DROP CONSTRAINT FK_Departments_created_by;
    IF OBJECT_ID('dbo.FK_Departments_updated_by', 'F') IS NOT NULL
        ALTER TABLE dbo.Departments DROP CONSTRAINT FK_Departments_updated_by;

    IF OBJECT_ID('dbo.FK_Companies_created_by', 'F') IS NOT NULL
        ALTER TABLE dbo.Companies DROP CONSTRAINT FK_Companies_created_by;
    IF OBJECT_ID('dbo.FK_Companies_updated_by', 'F') IS NOT NULL
        ALTER TABLE dbo.Companies DROP CONSTRAINT FK_Companies_updated_by;

    IF OBJECT_ID('dbo.FK_Users_created_by', 'F') IS NOT NULL
        ALTER TABLE dbo.Users DROP CONSTRAINT FK_Users_created_by;
    IF OBJECT_ID('dbo.FK_Users_updated_by', 'F') IS NOT NULL
        ALTER TABLE dbo.Users DROP CONSTRAINT FK_Users_updated_by;

    -- Drop Tables in reverse dependency order
    DROP TABLE IF EXISTS dbo.Employment_Histories;
    DROP TABLE IF EXISTS dbo.User_Department_Assignments;
    DROP TABLE IF EXISTS dbo.User_Company_Assignments;
    DROP TABLE IF EXISTS dbo.Departments;
    DROP TABLE IF EXISTS dbo.Companies;
    DROP TABLE IF EXISTS dbo.Users;

    -- Delete exactly the V0002 record
    DELETE FROM dbo.SchemaVersions WHERE Version = 'V0002';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO
