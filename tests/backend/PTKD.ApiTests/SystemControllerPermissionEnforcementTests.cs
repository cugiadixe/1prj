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
    public class SystemControllerPermissionEnforcementTests : IClassFixture<SafeTestWebApplicationFactory>
    {
        private readonly SafeTestWebApplicationFactory _factory;
        private readonly HttpClient _unauthenticatedClient;

        public SystemControllerPermissionEnforcementTests(SafeTestWebApplicationFactory factory)
        {
            _factory = factory;
            _unauthenticatedClient = factory.CreateClient();
        }

        [Fact]
        public async Task GetInfo_Unauthenticated_Returns401()
        {
            var response = await _unauthenticatedClient.GetAsync("/api/v2/system/info");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetInfo_AuthenticatedWithoutPermission_Returns403()
        {
            var client = await GetAuthenticatedClientAsync("sys_noperm");
            var response = await client.GetAsync("/api/v2/system/info");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetInfo_WithSecurityAdminManage_Returns200()
        {
            var (client, _) = await GetAuthenticatedClientWithPermissionAsync("sys_secadmin", "SECURITY_ADMIN_MANAGE");
            var response = await client.GetAsync("/api/v2/system/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetInfo_WithSecurityAdminManageAndXCompanyIdHeader_DoesNotReturn400()
        {
            var (client, _) = await GetAuthenticatedClientWithPermissionAsync("sys_xcompany", "SECURITY_ADMIN_MANAGE");
            client.DefaultRequestHeaders.Add("X-Company-Id", "1");
            var response = await client.GetAsync("/api/v2/system/info");
            // Global scope: X-Company-Id is ignored, not parsed. Response is 200 (permission granted), not 400.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetInfo_SecurityAuditViewAlone_Returns403()
        {
            var (client, _) = await GetAuthenticatedClientWithPermissionAsync("sys_auditview", "SECURITY_AUDIT_VIEW");
            var response = await client.GetAsync("/api/v2/system/info");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
