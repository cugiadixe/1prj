using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Api.Auth.Models;
using Xunit;

namespace PTKD.ApiTests
{
    [Collection("Sequential")]
    public class OrganizationPermissionEnforcementTests : IClassFixture<SafeTestWebApplicationFactory>
    {
        private readonly SafeTestWebApplicationFactory _factory;
        private readonly HttpClient _unauthenticatedClient;

        public OrganizationPermissionEnforcementTests(SafeTestWebApplicationFactory factory)
        {
            _factory = factory;
            _unauthenticatedClient = factory.CreateClient();
        }

        // ── UsersController ────────────────────────────────────────

        [Fact]
        public async Task Users_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.GetAsync("/api/v2/organizations/users");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Users_AuthenticatedWithoutPermission_Returns403()
        {
            var client = await GetAuthenticatedClientAsync("user_no_perm");
            var response = await client.GetAsync("/api/v2/organizations/users");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Users_AuthenticatedWithPermission_Succeeds()
        {
            var (client, userId) = await GetAuthenticatedClientWithPermissionAsync("user_with_perm", "ORGANIZATION_USER_MANAGE");
            await GrantPermissionAsync(userId, "ORGANIZATION_COMPANY_MANAGE");
            await GrantPermissionAsync(userId, "ORGANIZATION_DEPARTMENT_MANAGE");

            var compResp = await client.PostAsJsonAsync("/api/v2/organizations/companies", new { CompanyCode = "C_" + Guid.NewGuid().ToString("N")[..5], Name = "Test" });
            var compId = (await compResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt64();

            var deptResp = await client.PostAsJsonAsync("/api/v2/organizations/departments", new { DepartmentCode = "D_" + Guid.NewGuid().ToString("N")[..5], CompanyId = compId, Name = "Test" });
            var deptId = (await deptResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt64();
            
            // Read endpoint
            var response = await client.GetAsync("/api/v2/organizations/users");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Mutation endpoint (Create User)
            var empCode = "E_" + Guid.NewGuid().ToString("N")[..10];
            var createResponse = await client.PostAsJsonAsync(
                "/api/v2/organizations/users",
                new
                {
                    EmployeeCode = empCode,
                    FullName = "Test " + empCode,
                    EmploymentStatus = "Active",
                    AccountStatus = "Active",
                    InitialCompanyId = compId,
                    InitialDepartmentId = deptId,
                    EffectiveFrom = DateTime.UtcNow.ToString("o")
                });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        // ── DepartmentsController ──────────────────────────────────

        [Fact]
        public async Task Departments_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.GetAsync("/api/v2/organizations/departments");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Departments_AuthenticatedWithoutPermission_Returns403()
        {
            var client = await GetAuthenticatedClientAsync("dept_no_perm");
            var response = await client.GetAsync("/api/v2/organizations/departments");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Departments_AuthenticatedWithPermission_Succeeds()
        {
            var (client, userId) = await GetAuthenticatedClientWithPermissionAsync("dept_with_perm", "ORGANIZATION_DEPARTMENT_MANAGE");
            
            // Create a company to attach the department to
            await GrantPermissionAsync(userId, "ORGANIZATION_COMPANY_MANAGE"); // Needed to create company
            var code = "C_" + Guid.NewGuid().ToString("N")[..10];
            var compResp = await client.PostAsJsonAsync("/api/v2/organizations/companies", new { CompanyCode = code, Name = "Test" });
            compResp.EnsureSuccessStatusCode();
            var compContent = await compResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var companyId = compContent.GetProperty("id").GetInt64();

            // Read endpoint
            var response = await client.GetAsync($"/api/v2/organizations/departments?companyId={companyId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Mutation endpoint (Create Department)
            var deptCode = "D_" + Guid.NewGuid().ToString("N")[..10];
            var createResponse = await client.PostAsJsonAsync(
                "/api/v2/organizations/departments",
                new
                {
                    DepartmentCode = deptCode,
                    CompanyId = companyId,
                    Name = "Test Dept"
                });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        // ── CompaniesController ────────────────────────────────────

        [Fact]
        public async Task Companies_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.GetAsync("/api/v2/organizations/companies");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Companies_AuthenticatedWithoutPermission_Returns403()
        {
            var client = await GetAuthenticatedClientAsync("comp_no_perm");
            var response = await client.GetAsync("/api/v2/organizations/companies");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Companies_AuthenticatedWithPermission_Succeeds()
        {
            var (client, userId) = await GetAuthenticatedClientWithPermissionAsync("comp_with_perm", "ORGANIZATION_COMPANY_MANAGE");
            
            // Read endpoint
            var response = await client.GetAsync("/api/v2/organizations/companies");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Mutation endpoint (Create Company)
            var code = "C_" + Guid.NewGuid().ToString("N")[..10];
            var createResponse = await client.PostAsJsonAsync(
                "/api/v2/organizations/companies",
                new
                {
                    CompanyCode = code,
                    Name = "Test " + code
                });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        // ── Helpers ────────────────────────────────────────────────

        private async Task<HttpClient> GetAuthenticatedClientAsync(string baseUsername)
        {
            var (userId, token) = await SeedUserAndGetTokenAsync(baseUsername);
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, long UserId)> GetAuthenticatedClientWithPermissionAsync(string baseUsername, string permissionCode)
        {
            var (userId, token) = await SeedUserAndGetTokenAsync(baseUsername);
            await GrantPermissionAsync(userId, permissionCode);
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return (client, userId);
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

            var perm = System.Linq.Enumerable.FirstOrDefault(db.Set<PTKD.Domain.Security.Authorization.Permission>(), p => p.PermissionCode == permissionCode);
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
