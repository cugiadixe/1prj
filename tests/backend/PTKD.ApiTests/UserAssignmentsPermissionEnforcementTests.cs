using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Api.Auth.Models;
using Xunit;

namespace PTKD.ApiTests
{
    [Collection("Sequential")]
    public class UserAssignmentsPermissionEnforcementTests : IClassFixture<SafeTestWebApplicationFactory>
    {
        private readonly SafeTestWebApplicationFactory _factory;
        private readonly HttpClient _unauthenticatedClient;

        public UserAssignmentsPermissionEnforcementTests(SafeTestWebApplicationFactory factory)
        {
            _factory = factory;
            _unauthenticatedClient = factory.CreateClient();
        }

        // ── Unauthenticated → 401 (all 7 actions) ───────────────────────

        [Fact]
        public async Task AssignCompany_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.PostAsJsonAsync(
                "/api/v2/organizations/users/1/companies",
                new { CompanyId = 1, PrimaryDepartmentId = 1, EffectiveFrom = DateTime.UtcNow.ToString("o") });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AssignDepartment_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.PostAsJsonAsync(
                "/api/v2/organizations/users/1/departments",
                new { UserCompanyAssignmentId = 1, CompanyAssignmentRowVersion = "AAAAAAAAAAE=", DepartmentId = 1 });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePrimaryCompany_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.PutAsJsonAsync(
                "/api/v2/organizations/users/1/company-assignments/1/primary",
                new { TargetRowVersion = "AAAAAAAAAAE=", CurrentPrimaryAssignmentId = 1, CurrentPrimaryRowVersion = "AAAAAAAAAAE=" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePrimaryDepartment_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.PutAsJsonAsync(
                "/api/v2/organizations/users/1/department-assignments/1/primary",
                new { TargetRowVersion = "AAAAAAAAAAE=", CurrentPrimaryAssignmentId = 1, CurrentPrimaryRowVersion = "AAAAAAAAAAE=" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CloseCompanyAssignment_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.PutAsJsonAsync(
                "/api/v2/organizations/users/1/company-assignments/1/close",
                new { CompanyAssignmentRowVersion = "AAAAAAAAAAE=", EffectiveTo = DateTime.UtcNow.AddDays(1).ToString("o") });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SameCompanyDepartmentTransfer_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.PostAsJsonAsync(
                "/api/v2/organizations/users/1/company-assignments/1/transfer/same-company",
                new { CompanyAssignmentRowVersion = "AAAAAAAAAAE=", SourceDepartmentAssignmentId = 1, SourceDepartmentAssignmentRowVersion = "AAAAAAAAAAE=", TargetDepartmentId = 1, EffectiveDate = DateTime.UtcNow.ToString("o") });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CrossCompanyTransfer_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.PostAsJsonAsync(
                "/api/v2/organizations/users/1/company-assignments/1/transfer/cross-company",
                new { TargetCompanyId = 2, TargetDepartmentId = 1 });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ── Authenticated without ORGANIZATION_USER_MANAGE → 403 ────────

        [Fact]
        public async Task AssignCompany_AuthenticatedWithoutPermission_Returns403()
        {
            var client = await GetAuthenticatedClientAsync("ua_noperm");
            var response = await client.PostAsJsonAsync(
                "/api/v2/organizations/users/1/companies",
                new { CompanyId = 1, PrimaryDepartmentId = 1, EffectiveFrom = DateTime.UtcNow.ToString("o") });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ── SECURITY_ADMIN_MANAGE alone is not accepted → 403 ───────────

        [Fact]
        public async Task AssignCompany_SecurityAdminManageAlone_Returns403()
        {
            var (client, _) = await GetAuthenticatedClientWithPermissionAsync("ua_secadmin", "SECURITY_ADMIN_MANAGE");
            var response = await client.PostAsJsonAsync(
                "/api/v2/organizations/users/1/companies",
                new { CompanyId = 1, PrimaryDepartmentId = 1, EffectiveFrom = DateTime.UtcNow.ToString("o") });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ── X-Company-Id header does not cause 400 (Global scope ignores it) ─

        [Fact]
        public async Task AssignCompany_WithXCompanyIdHeader_DoesNotReturn400()
        {
            var client = await GetAuthenticatedClientAsync("ua_xcompany");
            client.DefaultRequestHeaders.Add("X-Company-Id", "1");
            var response = await client.PostAsJsonAsync(
                "/api/v2/organizations/users/1/companies",
                new { CompanyId = 1, PrimaryDepartmentId = 1, EffectiveFrom = DateTime.UtcNow.ToString("o") });
            // Global scope: X-Company-Id is ignored, not parsed. Response is 403 (no permission), not 400.
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ── Authenticated with ORGANIZATION_USER_MANAGE succeeds ─────────

        [Fact]
        public async Task AssignCompany_AuthenticatedWithPermission_Returns204()
        {
            var (client, userId) = await GetAuthenticatedClientWithPermissionAsync("ua_perm", "ORGANIZATION_USER_MANAGE");
            await GrantPermissionAsync(userId, "ORGANIZATION_COMPANY_MANAGE");
            await GrantPermissionAsync(userId, "ORGANIZATION_DEPARTMENT_MANAGE");

            var (company1Id, dept1Id) = await CreateCompanyAndDepartmentAsync(client);

            var empCode = "E_" + Guid.NewGuid().ToString("N")[..10];
            var createUserResp = await client.PostAsJsonAsync("/api/v2/organizations/users", new
            {
                EmployeeCode = empCode,
                FullName = "AssignTest " + empCode,
                EmploymentStatus = "Active",
                AccountStatus = "Active",
                InitialCompanyId = company1Id,
                InitialDepartmentId = dept1Id,
                EffectiveFrom = DateTime.UtcNow.ToString("o")
            });
            createUserResp.EnsureSuccessStatusCode();
            var (orgUserId, _, _) = await ParseResponseAsync(createUserResp);

            var (company2Id, dept2Id) = await CreateCompanyAndDepartmentAsync(client);

            var response = await client.PostAsJsonAsync(
                $"/api/v2/organizations/users/{orgUserId}/companies",
                new
                {
                    CompanyId = company2Id,
                    PrimaryDepartmentId = dept2Id,
                    EffectiveFrom = DateTime.UtcNow.ToString("o")
                });
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task SameCompanyDepartmentTransfer_AuthenticatedWithPermission_ReachesBusinessLayer()
        {
            var (client, _) = await GetAuthenticatedClientWithPermissionAsync("ua_transfer_perm", "ORGANIZATION_USER_MANAGE");
            var response = await client.PostAsJsonAsync(
                "/api/v2/organizations/users/999999/company-assignments/999999/transfer/same-company",
                new
                {
                    CompanyAssignmentRowVersion = "AAAAAAAAAAE=",
                    SourceDepartmentAssignmentId = 999999,
                    SourceDepartmentAssignmentRowVersion = "AAAAAAAAAAE=",
                    TargetDepartmentId = 1,
                    EffectiveDate = DateTime.UtcNow.ToString("o")
                });
            // Auth passed — business layer handles the request (404/409 are acceptable)
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private async Task<HttpClient> GetAuthenticatedClientAsync(string baseUsername)
        {
            var (_, token) = await SeedUserAndGetTokenAsync(baseUsername);
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, long UserId)> GetAuthenticatedClientWithPermissionAsync(
            string baseUsername, string permissionCode)
        {
            var (userId, token) = await SeedUserAndGetTokenAsync(baseUsername);
            await GrantPermissionAsync(userId, permissionCode);
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return (client, userId);
        }

        private async Task<(long CompanyId, long DeptId)> CreateCompanyAndDepartmentAsync(HttpClient client)
        {
            var compCode = "C_" + Guid.NewGuid().ToString("N")[..10];
            var compResp = await client.PostAsJsonAsync("/api/v2/organizations/companies",
                new { CompanyCode = compCode, Name = "Company " + compCode });
            compResp.EnsureSuccessStatusCode();
            var (compId, _, _) = await ParseResponseAsync(compResp);

            var deptCode = "D_" + Guid.NewGuid().ToString("N")[..10];
            var deptResp = await client.PostAsJsonAsync("/api/v2/organizations/departments",
                new { DepartmentCode = deptCode, CompanyId = compId, Name = "Dept " + deptCode });
            deptResp.EnsureSuccessStatusCode();
            var (deptId, _, _) = await ParseResponseAsync(deptResp);

            return (compId, deptId);
        }

        private static async Task<(long Id, string EmployeeCode, string RowVersion)> ParseResponseAsync(
            HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            return (
                Id: root.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0,
                EmployeeCode: root.TryGetProperty("employeeCode", out var ecProp) ? ecProp.GetString() ?? "" : "",
                RowVersion: root.TryGetProperty("rowVersion", out var rvProp) ? rvProp.GetString() ?? "" : ""
            );
        }

        private async Task<(long UserId, string Token)> SeedUserAndGetTokenAsync(string baseUsername)
        {
            using var scope = _factory.Services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
            using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

            var clock = scope.ServiceProvider.GetRequiredService<PTKD.Application.Security.Authentication.Interfaces.IUtcClock>();
            var hasher = scope.ServiceProvider.GetRequiredService<PTKD.Application.Security.Authentication.Interfaces.IPasswordHashService>();

            var username = baseUsername + Guid.NewGuid().ToString("N")[..5];
            var password = "TestPassword123!";

            var user = new PTKD.Domain.Entities.User(
                employeeCode: username,
                fullName: "Test User " + username,
                email: null,
                employmentStatus: "Active",
                accountStatus: "Active");

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var dummyAccount = PTKD.Domain.Entities.UserAuthAccount.CreateInternal(user.Id, username.ToUpperInvariant(), "TEMP", clock.UtcNow);
            var hash = hasher.HashPassword(dummyAccount, password);
            var account = PTKD.Domain.Entities.UserAuthAccount.CreateInternal(user.Id, username.ToUpperInvariant(), hash, clock.UtcNow);
            db.UserAuthAccounts.Add(account);
            await db.SaveChangesAsync();

            var loginReq = new LoginRequest(username, password);
            var authClient = _factory.CreateClient();
            var loginRes = await authClient.PostAsJsonAsync("/api/v2/auth/login", loginReq);
            loginRes.EnsureSuccessStatusCode();

            var body = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
            return (user.Id, body!.AccessToken);
        }

        private async Task GrantPermissionAsync(long userId, string permissionCode)
        {
            using var scope = _factory.Services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
            using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

            var perm = System.Linq.Enumerable.FirstOrDefault(
                db.Set<PTKD.Domain.Security.Authorization.Permission>(),
                p => p.PermissionCode == permissionCode);
            if (perm == null)
            {
                perm = new PTKD.Domain.Security.Authorization.Permission
                {
                    PermissionCode = permissionCode,
                    ModuleCode = "TEST",
                    ActionCode = "TEST",
                    DataScope = "GLOBAL",
                    IsSensitive = false,
                    RequiresReason = false,
                    IsDelegable = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    RowVersion = PTKD.Domain.ValueObjects.RowVersion.FromByteArray(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 })
                };
                db.Set<PTKD.Domain.Security.Authorization.Permission>().Add(perm);
                await db.SaveChangesAsync();
            }

            var up = new PTKD.Domain.Security.Authorization.UserIndividualPermission
            {
                UserId = userId,
                PermissionCode = permissionCode,
                ScopeType = "GLOBAL",
                CompanyId = null,
                GrantType = "ALLOW",
                AssignmentStatus = "ACTIVE",
                EffectiveFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                RowVersion = PTKD.Domain.ValueObjects.RowVersion.FromByteArray(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 })
            };
            db.Set<PTKD.Domain.Security.Authorization.UserIndividualPermission>().Add(up);
            await db.SaveChangesAsync();
        }
    }
}
