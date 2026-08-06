using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;

namespace PTKD.IntegrationTests
{
    [Collection("Sequential")]
    public class OrganizationSchemaTests
    {
        private readonly TestDatabaseFixture _fixture;

        public OrganizationSchemaTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void RejectionOfDuplicateCompanyCode()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var cmd1 = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) VALUES ('COMP1', 'Company 1', 1, SYSUTCDATETIME())", conn, trans);
            cmd1.ExecuteNonQuery();

            var cmd2 = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) VALUES ('COMP1', 'Company 2', 1, SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd2.ExecuteNonQuery());
            
            Assert.Contains("UQ_Companies_company_code", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RejectionOfDuplicateDepartmentCode()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var compCmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('COMP2', 'Company 2', 1, SYSUTCDATETIME())", conn, trans);
            var compId = (long)compCmd.ExecuteScalar();

            var cmd1 = new SqlCommand($"INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at) VALUES ('DEPT1', {compId}, 'Dept 1', 1, SYSUTCDATETIME())", conn, trans);
            cmd1.ExecuteNonQuery();

            var cmd2 = new SqlCommand($"INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at) VALUES ('DEPT1', {compId}, 'Dept 2', 1, SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd2.ExecuteNonQuery());

            Assert.Contains("UQ_Departments_department_code", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RejectionOfDuplicateEmployeeCode()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var cmd1 = new SqlCommand("INSERT INTO dbo.Users (employee_code, full_name, employment_status, account_status, created_at) VALUES ('EMP1', 'Emp 1', 'ACTIVE', 'ACTIVE', SYSUTCDATETIME())", conn, trans);
            cmd1.ExecuteNonQuery();

            var cmd2 = new SqlCommand("INSERT INTO dbo.Users (employee_code, full_name, employment_status, account_status, created_at) VALUES ('EMP1', 'Emp 2', 'ACTIVE', 'ACTIVE', SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd2.ExecuteNonQuery());

            Assert.Contains("UQ_Users_employee_code", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RowVersionChangingAfterUpdate()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id, INSERTED.row_version VALUES ('COMP3', 'Company 3', 1, SYSUTCDATETIME())", conn, trans);
            using var reader = cmd.ExecuteReader();
            reader.Read();
            var id = reader.GetInt64(0);
            var rv1 = (byte[])reader.GetValue(1);
            reader.Close();

            var updateCmd = new SqlCommand($"UPDATE dbo.Companies SET name = 'Updated' OUTPUT INSERTED.row_version WHERE id = {id}", conn, trans);
            var rv2 = (byte[])updateCmd.ExecuteScalar();

            Assert.NotEqual(rv1, rv2);
            trans.Rollback();
        }

        [Fact]
        public void NoOnDeleteCascadeForeignKeys()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.foreign_keys WHERE delete_referential_action = 1 AND parent_object_id IN (OBJECT_ID('dbo.Users'), OBJECT_ID('dbo.Companies'), OBJECT_ID('dbo.Departments'), OBJECT_ID('dbo.User_Company_Assignments'), OBJECT_ID('dbo.User_Department_Assignments'), OBJECT_ID('dbo.Employment_Histories'))", conn);
            var count = (int)cmd.ExecuteScalar();
            Assert.Equal(0, count);
        }

        [Fact]
        public void NoSeedData()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();

            var dropCmd = new SqlCommand("DELETE FROM dbo.Employment_Histories; DELETE FROM dbo.User_Department_Assignments; DELETE FROM dbo.User_Company_Assignments; DELETE FROM dbo.Departments; DELETE FROM dbo.Companies; DELETE FROM dbo.Users;", conn);
            dropCmd.ExecuteNonQuery();

            string[] tables = { "Users", "Companies", "Departments", "User_Company_Assignments", "User_Department_Assignments", "Employment_Histories" };
            foreach (var table in tables)
            {
                var cmd = new SqlCommand($"SELECT COUNT(*) FROM dbo.{table}", conn);
                var count = (int)cmd.ExecuteScalar();
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public void AllSixExpectedTablesExist()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();

            string[] tables = { "Users", "Companies", "Departments", "User_Company_Assignments", "User_Department_Assignments", "Employment_Histories" };
            foreach (var table in tables)
            {
                var cmd = new SqlCommand($"SELECT COUNT(*) FROM sys.tables WHERE name = '{table}' AND schema_id = SCHEMA_ID('dbo')", conn);
                var count = (int)cmd.ExecuteScalar();
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void AllExpectedFilteredIndexesExist()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();

            string[] indexes = { "UQ_User_Company_Active", "UQ_User_Primary_Company", "UQ_User_Dept_Active", "UQ_User_Company_Primary_Dept" };
            foreach (var idx in indexes)
            {
                var cmd = new SqlCommand($"SELECT COUNT(*) FROM sys.indexes WHERE name = '{idx}' AND has_filter = 1", conn);
                var count = (int)cmd.ExecuteScalar();
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void CompositeFkPreventsCrossUserCompanyMismatch()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var u1Cmd = new SqlCommand("INSERT INTO dbo.Users (employee_code, full_name, employment_status, account_status, created_at) OUTPUT INSERTED.id VALUES ('U1', 'U1', 'A', 'A', SYSUTCDATETIME())", conn, trans);
            var u1 = (long)u1Cmd.ExecuteScalar();

            var c1Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('C1', 'C1', 1, SYSUTCDATETIME())", conn, trans);
            var c1 = (long)c1Cmd.ExecuteScalar();

            var c2Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('C2', 'C2', 1, SYSUTCDATETIME())", conn, trans);
            var c2 = (long)c2Cmd.ExecuteScalar();

            var ucaCmd = new SqlCommand($"INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at) OUTPUT INSERTED.id VALUES ({u1}, {c1}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            var uca1 = (long)ucaCmd.ExecuteScalar();

            var d2Cmd = new SqlCommand($"INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('D2', {c2}, 'D2', 1, SYSUTCDATETIME())", conn, trans);
            var d2 = (long)d2Cmd.ExecuteScalar();

            // Attempt to assign user to department D2 (which is in C2) using the UCA for C1.
            var cmd = new SqlCommand($"INSERT INTO dbo.User_Department_Assignments (user_id, department_id, user_company_assignment_id, company_id, is_primary_for_company, assignment_status, effective_from, created_at) VALUES ({u1}, {d2}, {uca1}, {c2}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());

            Assert.Contains("FK_UserDepartmentAssignments_company_assignment", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RejectionOfCrossCompanyParentDepartment()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var c1Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CA1', 'CA1', 1, SYSUTCDATETIME())", conn, trans);
            var c1 = (long)c1Cmd.ExecuteScalar();

            var c2Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CA2', 'CA2', 1, SYSUTCDATETIME())", conn, trans);
            var c2 = (long)c2Cmd.ExecuteScalar();

            var d1Cmd = new SqlCommand($"INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('DA1', {c1}, 'DA1', 1, SYSUTCDATETIME())", conn, trans);
            var d1 = (long)d1Cmd.ExecuteScalar();

            // Department in C2 with parent department in C1
            var cmd = new SqlCommand($"INSERT INTO dbo.Departments (department_code, company_id, parent_department_id, name, is_active, created_at) VALUES ('DA2', {c2}, {d1}, 'DA2', 1, SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());

            Assert.Contains("FK_Departments_parent_department_id", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RejectionOfTwoActivePrimaryDepartmentsForOneUserCompany()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var u1Cmd = new SqlCommand("INSERT INTO dbo.Users (employee_code, full_name, employment_status, account_status, created_at) OUTPUT INSERTED.id VALUES ('UB1', 'UB1', 'A', 'A', SYSUTCDATETIME())", conn, trans);
            var u1 = (long)u1Cmd.ExecuteScalar();

            var c1Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CB1', 'CB1', 1, SYSUTCDATETIME())", conn, trans);
            var c1 = (long)c1Cmd.ExecuteScalar();

            var ucaCmd = new SqlCommand($"INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at) OUTPUT INSERTED.id VALUES ({u1}, {c1}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            var uca1 = (long)ucaCmd.ExecuteScalar();

            var d1Cmd = new SqlCommand($"INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('DB1', {c1}, 'DB1', 1, SYSUTCDATETIME())", conn, trans);
            var d1 = (long)d1Cmd.ExecuteScalar();

            var d2Cmd = new SqlCommand($"INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('DB2', {c1}, 'DB2', 1, SYSUTCDATETIME())", conn, trans);
            var d2 = (long)d2Cmd.ExecuteScalar();

            var uda1Cmd = new SqlCommand($"INSERT INTO dbo.User_Department_Assignments (user_id, department_id, user_company_assignment_id, company_id, is_primary_for_company, assignment_status, effective_from, created_at) VALUES ({u1}, {d1}, {uca1}, {c1}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            uda1Cmd.ExecuteNonQuery();

            var cmd = new SqlCommand($"INSERT INTO dbo.User_Department_Assignments (user_id, department_id, user_company_assignment_id, company_id, is_primary_for_company, assignment_status, effective_from, created_at) VALUES ({u1}, {d2}, {uca1}, {c1}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());

            Assert.Contains("UQ_User_Company_Primary_Dept", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RejectionOfActiveWithNonNullEffectiveTo()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var u1Cmd = new SqlCommand("INSERT INTO dbo.Users (employee_code, full_name, employment_status, account_status, created_at) OUTPUT INSERTED.id VALUES ('UC1', 'UC1', 'A', 'A', SYSUTCDATETIME())", conn, trans);
            var u1 = (long)u1Cmd.ExecuteScalar();

            var c1Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CC1', 'CC1', 1, SYSUTCDATETIME())", conn, trans);
            var c1 = (long)c1Cmd.ExecuteScalar();

            var cmd = new SqlCommand($"INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, effective_to, created_at) VALUES ({u1}, {c1}, 1, 'ACTIVE', '2025-01-01', '2025-01-02', SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());

            Assert.Contains("CK_UserCompanyAssignments_StatusConsistency", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RejectionOfEffectiveToLessThanEffectiveFrom()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var u1Cmd = new SqlCommand("INSERT INTO dbo.Users (employee_code, full_name, employment_status, account_status, created_at) OUTPUT INSERTED.id VALUES ('UD1', 'UD1', 'A', 'A', SYSUTCDATETIME())", conn, trans);
            var u1 = (long)u1Cmd.ExecuteScalar();

            var c1Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CD1', 'CD1', 1, SYSUTCDATETIME())", conn, trans);
            var c1 = (long)c1Cmd.ExecuteScalar();

            var cmd = new SqlCommand($"INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, effective_to, created_at) VALUES ({u1}, {c1}, 1, 'CLOSED', '2025-01-02', '2025-01-01', SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd.ExecuteNonQuery());

            Assert.Contains("CK_UserCompanyAssignments_EffectiveDates", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RejectionOfDirectSelfParentReferences()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CE1', 'CE1', 1, SYSUTCDATETIME())", conn, trans);
            var c1 = (long)cmd.ExecuteScalar();

            var updateCmd = new SqlCommand($"UPDATE dbo.Companies SET parent_company_id = {c1} WHERE id = {c1}", conn, trans);
            var ex = Assert.Throws<SqlException>(() => updateCmd.ExecuteNonQuery());

            Assert.Contains("CK_Companies_NoDirectSelfParent", ex.Message);
            trans.Rollback();
        }
        
        [Fact]
        public void RejectionOfTwoActiveCompanyAssignmentsForSameUserAndCompany()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var u1Cmd = new SqlCommand("INSERT INTO dbo.Users (employee_code, full_name, employment_status, account_status, created_at) OUTPUT INSERTED.id VALUES ('UF1', 'UF1', 'A', 'A', SYSUTCDATETIME())", conn, trans);
            var u1 = (long)u1Cmd.ExecuteScalar();

            var c1Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CF1', 'CF1', 1, SYSUTCDATETIME())", conn, trans);
            var c1 = (long)c1Cmd.ExecuteScalar();

            var ucaCmd = new SqlCommand($"INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at) VALUES ({u1}, {c1}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            ucaCmd.ExecuteNonQuery();

            var cmd2 = new SqlCommand($"INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at) VALUES ({u1}, {c1}, 0, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd2.ExecuteNonQuery());

            Assert.Contains("UQ_User_Company_Active", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RejectionOfTwoActivePrimaryCompaniesForSameUser()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var u1Cmd = new SqlCommand("INSERT INTO dbo.Users (employee_code, full_name, employment_status, account_status, created_at) OUTPUT INSERTED.id VALUES ('UG1', 'UG1', 'A', 'A', SYSUTCDATETIME())", conn, trans);
            var u1 = (long)u1Cmd.ExecuteScalar();

            var c1Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CG1', 'CG1', 1, SYSUTCDATETIME())", conn, trans);
            var c1 = (long)c1Cmd.ExecuteScalar();

            var c2Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CG2', 'CG2', 1, SYSUTCDATETIME())", conn, trans);
            var c2 = (long)c2Cmd.ExecuteScalar();

            var ucaCmd = new SqlCommand($"INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at) VALUES ({u1}, {c1}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            ucaCmd.ExecuteNonQuery();

            var cmd2 = new SqlCommand($"INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at) VALUES ({u1}, {c2}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd2.ExecuteNonQuery());

            Assert.Contains("UQ_User_Primary_Company", ex.Message);
            trans.Rollback();
        }

        [Fact]
        public void RejectionOfTwoActiveAssignmentsForSameUserAndDepartment()
        {
            using var conn = new SqlConnection(_fixture.ConnectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            var u1Cmd = new SqlCommand("INSERT INTO dbo.Users (employee_code, full_name, employment_status, account_status, created_at) OUTPUT INSERTED.id VALUES ('UH1', 'UH1', 'A', 'A', SYSUTCDATETIME())", conn, trans);
            var u1 = (long)u1Cmd.ExecuteScalar();

            var c1Cmd = new SqlCommand("INSERT INTO dbo.Companies (company_code, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('CH1', 'CH1', 1, SYSUTCDATETIME())", conn, trans);
            var c1 = (long)c1Cmd.ExecuteScalar();

            var ucaCmd = new SqlCommand($"INSERT INTO dbo.User_Company_Assignments (user_id, company_id, is_primary, assignment_status, effective_from, created_at) OUTPUT INSERTED.id VALUES ({u1}, {c1}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            var uca1 = (long)ucaCmd.ExecuteScalar();

            var d1Cmd = new SqlCommand($"INSERT INTO dbo.Departments (department_code, company_id, name, is_active, created_at) OUTPUT INSERTED.id VALUES ('DH1', {c1}, 'DH1', 1, SYSUTCDATETIME())", conn, trans);
            var d1 = (long)d1Cmd.ExecuteScalar();

            var uda1Cmd = new SqlCommand($"INSERT INTO dbo.User_Department_Assignments (user_id, department_id, user_company_assignment_id, company_id, is_primary_for_company, assignment_status, effective_from, created_at) VALUES ({u1}, {d1}, {uca1}, {c1}, 1, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            uda1Cmd.ExecuteNonQuery();

            var cmd2 = new SqlCommand($"INSERT INTO dbo.User_Department_Assignments (user_id, department_id, user_company_assignment_id, company_id, is_primary_for_company, assignment_status, effective_from, created_at) VALUES ({u1}, {d1}, {uca1}, {c1}, 0, 'ACTIVE', SYSUTCDATETIME(), SYSUTCDATETIME())", conn, trans);
            var ex = Assert.Throws<SqlException>(() => cmd2.ExecuteNonQuery());

            Assert.Contains("UQ_User_Dept_Active", ex.Message);
            trans.Rollback();
        }
    }
}
