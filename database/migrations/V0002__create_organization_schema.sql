CREATE TABLE dbo.Users (
    id bigint IDENTITY(1,1) NOT NULL,
    employee_code varchar(50) NOT NULL,
    full_name nvarchar(200) NOT NULL,
    email varchar(200) NULL,
    employment_status varchar(30) NOT NULL,
    account_status varchar(30) NOT NULL,
    row_version rowversion NOT NULL,
    created_at datetime2(3) NOT NULL,
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    CONSTRAINT PK_Users PRIMARY KEY (id),
    CONSTRAINT UQ_Users_employee_code UNIQUE (employee_code)
);
GO

CREATE TABLE dbo.Companies (
    id bigint IDENTITY(1,1) NOT NULL,
    company_code varchar(50) NOT NULL,
    parent_company_id bigint NULL,
    name nvarchar(200) NOT NULL,
    tax_code varchar(50) NULL,
    is_active bit NOT NULL,
    row_version rowversion NOT NULL,
    created_at datetime2(3) NOT NULL,
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    CONSTRAINT PK_Companies PRIMARY KEY (id),
    CONSTRAINT UQ_Companies_company_code UNIQUE (company_code),
    CONSTRAINT FK_Companies_parent_company_id FOREIGN KEY (parent_company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT CK_Companies_NoDirectSelfParent CHECK (parent_company_id <> id)
);
GO

CREATE TABLE dbo.Departments (
    id bigint IDENTITY(1,1) NOT NULL,
    department_code varchar(50) NOT NULL,
    company_id bigint NOT NULL,
    parent_department_id bigint NULL,
    name nvarchar(200) NOT NULL,
    is_active bit NOT NULL,
    row_version rowversion NOT NULL,
    created_at datetime2(3) NOT NULL,
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    CONSTRAINT PK_Departments PRIMARY KEY (id),
    CONSTRAINT UQ_Departments_department_code UNIQUE (department_code),
    CONSTRAINT UQ_Departments_Id_CompanyId UNIQUE (id, company_id),
    CONSTRAINT FK_Departments_company_id FOREIGN KEY (company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT FK_Departments_parent_department_id FOREIGN KEY (parent_department_id, company_id) REFERENCES dbo.Departments(id, company_id),
    CONSTRAINT CK_Departments_NoDirectSelfParent CHECK (parent_department_id <> id)
);
GO

CREATE TABLE dbo.User_Company_Assignments (
    id bigint IDENTITY(1,1) NOT NULL,
    user_id bigint NOT NULL,
    company_id bigint NOT NULL,
    is_primary bit NOT NULL,
    assignment_status varchar(30) NOT NULL,
    effective_from datetime2(3) NOT NULL,
    effective_to datetime2(3) NULL,
    row_version rowversion NOT NULL,
    created_at datetime2(3) NOT NULL,
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    CONSTRAINT PK_User_Company_Assignments PRIMARY KEY (id),
    CONSTRAINT UQ_UserCompanyAssignments_Id_UserId_CompanyId UNIQUE (id, user_id, company_id),
    CONSTRAINT FK_UserCompanyAssignments_user_id FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserCompanyAssignments_company_id FOREIGN KEY (company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT CK_UserCompanyAssignments_EffectiveDates CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_UserCompanyAssignments_StatusConsistency CHECK ((assignment_status = 'ACTIVE' AND effective_to IS NULL) OR (assignment_status = 'CLOSED' AND effective_to IS NOT NULL)),
    CONSTRAINT CK_UserCompanyAssignments_AssignmentStatus CHECK (assignment_status IN ('ACTIVE', 'CLOSED'))
);
GO

CREATE TABLE dbo.User_Department_Assignments (
    id bigint IDENTITY(1,1) NOT NULL,
    user_id bigint NOT NULL,
    department_id bigint NOT NULL,
    user_company_assignment_id bigint NOT NULL,
    company_id bigint NOT NULL,
    is_primary_for_company bit NOT NULL,
    assignment_status varchar(30) NOT NULL,
    effective_from datetime2(3) NOT NULL,
    effective_to datetime2(3) NULL,
    row_version rowversion NOT NULL,
    created_at datetime2(3) NOT NULL,
    created_by_user_id bigint NULL,
    updated_at datetime2(3) NULL,
    updated_by_user_id bigint NULL,
    CONSTRAINT PK_User_Department_Assignments PRIMARY KEY (id),
    CONSTRAINT FK_UserDepartmentAssignments_user_id FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_UserDepartmentAssignments_department_id_company_id FOREIGN KEY (department_id, company_id) REFERENCES dbo.Departments(id, company_id),
    CONSTRAINT FK_UserDepartmentAssignments_company_assignment FOREIGN KEY (user_company_assignment_id, user_id, company_id) REFERENCES dbo.User_Company_Assignments(id, user_id, company_id),
    CONSTRAINT CK_UserDepartmentAssignments_EffectiveDates CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_UserDepartmentAssignments_StatusConsistency CHECK ((assignment_status = 'ACTIVE' AND effective_to IS NULL) OR (assignment_status = 'CLOSED' AND effective_to IS NOT NULL)),
    CONSTRAINT CK_UserDepartmentAssignments_AssignmentStatus CHECK (assignment_status IN ('ACTIVE', 'CLOSED'))
);
GO

CREATE TABLE dbo.Employment_Histories (
    id bigint IDENTITY(1,1) NOT NULL,
    user_id bigint NOT NULL,
    from_company_id bigint NULL,
    to_company_id bigint NULL,
    from_department_id bigint NULL,
    to_department_id bigint NULL,
    from_company_assignment_id bigint NULL,
    to_company_assignment_id bigint NULL,
    from_department_assignment_id bigint NULL,
    to_department_assignment_id bigint NULL,
    action_type varchar(50) NOT NULL,
    reason nvarchar(500) NULL,
    effective_date datetime2(3) NOT NULL,
    correlation_id uniqueidentifier NULL,
    created_at datetime2(3) NOT NULL,
    created_by_user_id bigint NULL,
    CONSTRAINT PK_Employment_Histories PRIMARY KEY (id),
    CONSTRAINT FK_EmploymentHistories_user_id FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
    CONSTRAINT FK_EmploymentHistories_from_company_id FOREIGN KEY (from_company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT FK_EmploymentHistories_to_company_id FOREIGN KEY (to_company_id) REFERENCES dbo.Companies(id),
    CONSTRAINT FK_EmploymentHistories_from_department_id FOREIGN KEY (from_department_id) REFERENCES dbo.Departments(id),
    CONSTRAINT FK_EmploymentHistories_to_department_id FOREIGN KEY (to_department_id) REFERENCES dbo.Departments(id),
    CONSTRAINT FK_EmploymentHistories_from_company_assignment_id FOREIGN KEY (from_company_assignment_id) REFERENCES dbo.User_Company_Assignments(id),
    CONSTRAINT FK_EmploymentHistories_to_company_assignment_id FOREIGN KEY (to_company_assignment_id) REFERENCES dbo.User_Company_Assignments(id),
    CONSTRAINT FK_EmploymentHistories_from_department_assignment_id FOREIGN KEY (from_department_assignment_id) REFERENCES dbo.User_Department_Assignments(id),
    CONSTRAINT FK_EmploymentHistories_to_department_assignment_id FOREIGN KEY (to_department_assignment_id) REFERENCES dbo.User_Department_Assignments(id)
);
GO

-- Filtered Indexes
CREATE UNIQUE INDEX UQ_User_Company_Active 
ON dbo.User_Company_Assignments(user_id, company_id) 
WHERE assignment_status = 'ACTIVE';
GO

CREATE UNIQUE INDEX UQ_User_Primary_Company 
ON dbo.User_Company_Assignments(user_id) 
WHERE assignment_status = 'ACTIVE' AND is_primary = 1;
GO

CREATE UNIQUE INDEX UQ_User_Dept_Active 
ON dbo.User_Department_Assignments(user_id, department_id) 
WHERE assignment_status = 'ACTIVE';
GO

CREATE UNIQUE INDEX UQ_User_Company_Primary_Dept 
ON dbo.User_Department_Assignments(user_id, company_id) 
WHERE assignment_status = 'ACTIVE' AND is_primary_for_company = 1;
GO

-- ALTER TABLES to add created/updated by foreign keys at the end
ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id);
ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id);

ALTER TABLE dbo.Companies ADD CONSTRAINT FK_Companies_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id);
ALTER TABLE dbo.Companies ADD CONSTRAINT FK_Companies_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id);

ALTER TABLE dbo.Departments ADD CONSTRAINT FK_Departments_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id);
ALTER TABLE dbo.Departments ADD CONSTRAINT FK_Departments_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id);

ALTER TABLE dbo.User_Company_Assignments ADD CONSTRAINT FK_UserCompanyAssignments_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id);
ALTER TABLE dbo.User_Company_Assignments ADD CONSTRAINT FK_UserCompanyAssignments_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id);

ALTER TABLE dbo.User_Department_Assignments ADD CONSTRAINT FK_UserDepartmentAssignments_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id);
ALTER TABLE dbo.User_Department_Assignments ADD CONSTRAINT FK_UserDepartmentAssignments_updated_by FOREIGN KEY (updated_by_user_id) REFERENCES dbo.Users(id);

ALTER TABLE dbo.Employment_Histories ADD CONSTRAINT FK_EmploymentHistories_created_by FOREIGN KEY (created_by_user_id) REFERENCES dbo.Users(id);
GO
