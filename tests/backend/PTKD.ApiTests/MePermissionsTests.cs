using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Api.Auth.Models;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Domain.Security.Authentication;
using PTKD.Domain.Security.Authorization;
using PTKD.IntegrationTests;

namespace PTKD.ApiTests;

[Collection("Sequential")]
public class MePermissionsTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MePermissionsTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentUserPermissions_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v2/auth/me/permissions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUserPermissions_Authenticated_ReturnsExpectedShape()
    {
        var testUsername = "api_perm_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        var (userId, _) = await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/auth/me/permissions");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var result = await res.Content.ReadFromJsonAsync<CurrentUserPermissionsResponseDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.Permissions);

        var json = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("role", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adminGroup", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lineage", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ENTITY", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCurrentUserPermissions_ScopesAndDenyWins()
    {
        var testUsername = "api_perm_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        var (userId, accountId) = await SeedUserAndAccountAsync(testUsername, testPassword);

        await AssignPermissionsAsync(userId, 1L);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        // 1. Without X-Company-Id -> GLOBAL only
        var req1 = new HttpRequestMessage(HttpMethod.Get, "/api/v2/auth/me/permissions");
        req1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var res1 = await _client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var result1 = await res1.Content.ReadFromJsonAsync<CurrentUserPermissionsResponseDto>();
        var perms = result1!.Permissions;
        Assert.Contains(perms, p => p.PermissionCode == "TEST_PERM_1" && p.Scope == "GLOBAL");
        Assert.DoesNotContain(perms, p => p.PermissionCode == "TEST_PERM_2");

        // 2. With X-Company-Id -> GLOBAL + COMPANY
        var req2 = new HttpRequestMessage(HttpMethod.Get, "/api/v2/auth/me/permissions");
        req2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        req2.Headers.Add("X-Company-Id", "1");
        var res2 = await _client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var result2 = await res2.Content.ReadFromJsonAsync<CurrentUserPermissionsResponseDto>();
        var perms2 = result2!.Permissions;

        Assert.Contains(perms2, p => p.PermissionCode == "TEST_PERM_1" && p.Scope == "GLOBAL");
        Assert.DoesNotContain(perms2, p => p.PermissionCode == "TEST_PERM_2"); // DENY-wins
    }

    private async Task<(long UserId, long AccountId)> SeedUserAndAccountAsync(string username, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        var authContextFactory = scope.ServiceProvider.GetRequiredService<IAuthenticationDbContextFactory>();
        var hashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
        var clock = scope.ServiceProvider.GetRequiredService<IUtcClock>();

        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbContextFactory.CreateDbContext();

        var user = new PTKD.Domain.Entities.User(username, "Test " + username, username + "@test.internal", "ACTIVE", "ACTIVE");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hasher = new PasswordHasher<PTKD.Domain.Entities.UserAuthAccount>();
        var hash = hasher.HashPassword(null!, password);

        var account = PTKD.Domain.Entities.UserAuthAccount.CreateInternal(user.Id, username.ToUpperInvariant(), hash, clock.UtcNow);
        db.UserAuthAccounts.Add(account);
        await db.SaveChangesAsync();

        return (user.Id, account.Id);
    }

    private async Task AssignPermissionsAsync(long userId, long companyId)
    {
        using var scope = _factory.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<ISecurityAdminService>();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbContextFactory.CreateDbContext();

        if (!db.Permissions.Any(x => x.PermissionCode == "TEST_PERM_1"))
        {
            db.Permissions.Add(new PTKD.Domain.Security.Authorization.Permission { PermissionCode = "TEST_PERM_1", ModuleCode = "TEST", ActionCode = "TEST", DataScope = "GLOBAL", IsActive = true });
        }
        if (!db.Permissions.Any(x => x.PermissionCode == "TEST_PERM_2"))
        {
            db.Permissions.Add(new PTKD.Domain.Security.Authorization.Permission { PermissionCode = "TEST_PERM_2", ModuleCode = "TEST", ActionCode = "TEST", DataScope = "GLOBAL", IsActive = true });
        }
        await db.SaveChangesAsync();

        var effectiveFrom = DateTime.UtcNow.AddDays(-1);
        await adminService.GrantIndividualPermissionAsync(1, userId, new CreateUserIndividualPermissionRequest("TEST_PERM_1", "GLOBAL", null, "ALLOW", effectiveFrom, null, "test"));
        await adminService.GrantIndividualPermissionAsync(1, userId, new CreateUserIndividualPermissionRequest("TEST_PERM_2", "GLOBAL", null, "ALLOW", effectiveFrom, null, "test"));
        await adminService.GrantIndividualPermissionAsync(1, userId, new CreateUserIndividualPermissionRequest("TEST_PERM_2", "GLOBAL", null, "DENY", effectiveFrom, null, "test")); // DENY wins
    }
}
