using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Application.Common.Models;
using PTKD.Application.Security.AccountManagement.DTOs;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;
using PTKD.Infrastructure.Persistence;

namespace PTKD.ApiTests.Security;

[Collection("Sequential")]
public sealed class AccountDiscoveryApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public AccountDiscoveryApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── SearchAccounts — authorization ────────────────────────────────────────

    [Fact]
    public async Task SearchAccounts_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v2/security/accounts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchAccounts_WithoutPermission_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v2/security/accounts");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── SearchAccounts — happy path ───────────────────────────────────────────

    [Fact]
    public async Task SearchAccounts_WithPermission_Returns200WithPagedResult()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync("/api/v2/security/accounts?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AccountSummaryDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.True(result.TotalCount >= 0);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task SearchAccounts_ReturnsAccountWithCorrectFields()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.UserAuthAccounts.AsNoTracking().FirstAsync(a => a.UserId == userId);

        // Search by providerSubject (username) to avoid pagination issues in a populated test DB.
        var response = await client.GetAsync($"/api/v2/security/accounts?search={Uri.EscapeDataString(account.ProviderSubject)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AccountSummaryDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);

        var summary = result.Items.FirstOrDefault(x => x.AccountId == account.Id);
        Assert.NotNull(summary);
        Assert.Equal(account.Id, summary.AccountId);
        Assert.Equal(userId, summary.UserId);
        Assert.Equal("INTERNAL", summary.ProviderType);
        Assert.Equal("ACTIVE", summary.Status);
    }

    [Fact]
    public async Task SearchAccounts_FilterByStatus_ReturnsMatchingOnly()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        await CreateDisabledAccountAsync(userId);

        var response = await client.GetAsync("/api/v2/security/accounts?status=DISABLED");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AccountSummaryDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Equal("DISABLED", item.Status));
    }

    [Fact]
    public async Task SearchAccounts_FilterBySearch_ReturnsMatchingUsername()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.UserAuthAccounts.AsNoTracking().FirstAsync(a => a.UserId == userId);
        var searchTerm = account.ProviderSubject;

        var response = await client.GetAsync($"/api/v2/security/accounts?search={Uri.EscapeDataString(searchTerm)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AccountSummaryDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Contains(result.Items, item => item.AccountId == account.Id);
    }

    [Fact]
    public async Task SearchAccounts_Pagination_DefaultsApplied()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync("/api/v2/security/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<AccountSummaryDto>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    // ── SearchAccounts — validation ───────────────────────────────────────────

    [Fact]
    public async Task SearchAccounts_PageZero_Returns400()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync("/api/v2/security/accounts?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("PAGE_INVALID", problem.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public async Task SearchAccounts_PageSizeOverMax_Returns400()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync("/api/v2/security/accounts?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("PAGE_SIZE_INVALID", problem.Extensions["errorCode"]?.ToString());
    }

    // ── SearchAccounts — data safety ──────────────────────────────────────────

    [Fact]
    public async Task SearchAccounts_ResponseJson_DoesNotExposeForbiddenFields()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync("/api/v2/security/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password_hash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("security_stamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rowVersion", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionsInvalidatedAt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"email\"", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetAccountsByUserId — authorization ───────────────────────────────────

    [Fact]
    public async Task GetAccountsByUserId_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v2/security/accounts/by-user/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountsByUserId_WithoutPermission_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v2/security/accounts/by-user/1");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── GetAccountsByUserId — happy path ──────────────────────────────────────

    [Fact]
    public async Task GetAccountsByUserId_ExistingUser_Returns200WithAccounts()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync($"/api/v2/security/accounts/by-user/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var accounts = await response.Content.ReadFromJsonAsync<AccountSummaryDto[]>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(accounts);
        Assert.NotEmpty(accounts);
        Assert.All(accounts, a => Assert.Equal(userId, a.UserId));
    }

    [Fact]
    public async Task GetAccountsByUserId_ReturnsAccountIdMappingForUser()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var expectedAccount = await db.UserAuthAccounts.AsNoTracking().FirstAsync(a => a.UserId == userId);

        var response = await client.GetAsync($"/api/v2/security/accounts/by-user/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var accounts = await response.Content.ReadFromJsonAsync<AccountSummaryDto[]>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(accounts);

        var match = accounts.FirstOrDefault(a => a.AccountId == expectedAccount.Id);
        Assert.NotNull(match);
        Assert.Equal(expectedAccount.Id, match.AccountId);
        Assert.Equal(userId, match.UserId);
    }

    [Fact]
    public async Task GetAccountsByUserId_UserExistsNoAccounts_Returns200WithEmptyArray()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var userId = await CreateUserWithNoAccountAsync();

        var response = await client.GetAsync($"/api/v2/security/accounts/by-user/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var accounts = await response.Content.ReadFromJsonAsync<AccountSummaryDto[]>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(accounts);
        Assert.Empty(accounts);
    }

    // ── GetAccountsByUserId — not found ───────────────────────────────────────

    [Fact]
    public async Task GetAccountsByUserId_UserNotFound_Returns404()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync("/api/v2/security/accounts/by-user/999999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("USER_NOT_FOUND", problem.Extensions["errorCode"]?.ToString());
    }

    // ── GetAccountsByUserId — data safety ─────────────────────────────────────

    [Fact]
    public async Task GetAccountsByUserId_ResponseJson_DoesNotExposeForbiddenFields()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync($"/api/v2/security/accounts/by-user/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password_hash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("security_stamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rowVersion", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionsInvalidatedAt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"email\"", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── Route disambiguation ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAccountDetail_StillWorksAfterDiscoveryEndpointsAdded()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.UserAuthAccounts.AsNoTracking().FirstAsync(a => a.UserId == userId);

        var response = await client.GetAsync($"/api/v2/security/accounts/{account.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<long> CreateUserWithNoAccountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"noact_{uid}", "No Account User", $"noact_{uid}@ptkd.local", "ACTIVE", "ACTIVE");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<long> CreateDisabledAccountAsync(long callerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var uid = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"dis2_{uid}", "Disabled User", $"dis2_{uid}@ptkd.local", "ACTIVE", "ACTIVE");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var account = UserAuthAccount.CreateInternal(user.Id, $"dis2_{uid}", "hash_disabled", now);
        account.Disable(now, callerUserId);
        db.UserAuthAccounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }
}
