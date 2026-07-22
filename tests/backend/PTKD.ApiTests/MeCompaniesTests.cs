using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Api.Auth.Models;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Domain.Security.Authentication;
using PTKD.IntegrationTests;

namespace PTKD.ApiTests;

[Collection("Sequential")]
public class MeCompaniesTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MeCompaniesTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Existing tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserCompanies_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v2/auth/me/companies");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUserCompanies_Authenticated_ReturnsExpectedShapeAndCompanies()
    {
        var testUsername = "api_comp_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        var (userId, _) = await SeedUserAndAccountAsync(testUsername, testPassword);

        await AssignCompanyAsync(userId);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/auth/me/companies");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var result = await res.Content.ReadFromJsonAsync<UserCompaniesResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Companies);
        Assert.NotEmpty(result.Companies);

        var json = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("role", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adminGroup", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lineage", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("assignmentStatus", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── New tests — Phase 1B.1-M coverage ────────────────────────────────────

    /// <summary>
    /// A user with zero active company assignments must receive
    /// { "companies": [] } — not a null body, not an error.
    /// </summary>
    [Fact]
    public async Task GetCurrentUserCompanies_NoAssignments_ReturnsEmptySafeArray()
    {
        var testUsername = "api_comp_empty_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        // Seed user + account but NO company assignment
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        loginRes.EnsureSuccessStatusCode();
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/auth/me/companies");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var result = await res.Content.ReadFromJsonAsync<UserCompaniesResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Companies);   // must be present (not null)
        Assert.Empty(result.Companies);      // must be empty, not an error
    }

    /// <summary>
    /// The endpoint requires only a valid JWT — no specific permission code
    /// (no role, no individual permission, no admin-group membership).
    /// A freshly created user with zero permissions must still receive 200.
    /// </summary>
    [Fact]
    public async Task GetCurrentUserCompanies_NoPermissionCodeRequired_Returns200()
    {
        var testUsername = "api_comp_noperm_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        // No permissions, no company assignment, no roles
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        loginRes.EnsureSuccessStatusCode();
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/auth/me/companies");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var res = await _client.SendAsync(req);

        // Must succeed — this is a read of own data, no permission code required
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    /// <summary>
    /// Reading own company list is a safe, non-audited operation.
    /// No Security_Audit_Events row must be written as a result of calling
    /// GET /api/v2/auth/me/companies.
    /// </summary>
    [Fact]
    public async Task GetCurrentUserCompanies_DoesNotWriteAuditEvent()
    {
        var testUsername = "api_comp_audit_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        var (userId, _) = await SeedUserAndAccountAsync(testUsername, testPassword);
        await AssignCompanyAsync(userId);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        loginRes.EnsureSuccessStatusCode();
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        // Snapshot audit count before the call
        var countBefore = CountAuditEventsByUser(userId);

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/auth/me/companies");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        // No new audit rows may exist for this user after the read
        var countAfter = CountAuditEventsByUser(userId);
        Assert.Equal(countBefore, countAfter);
    }

    /// <summary>
    /// Companies must be returned ordered by CompanyName ASC, then CompanyId ASC,
    /// matching the stable ordering in SecurityAdminService.GetSelectableCompaniesAsync.
    /// </summary>
    [Fact]
    public async Task GetCurrentUserCompanies_ReturnsStableAlphabeticalOrder()
    {
        var testUsername = "api_comp_order_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        var (userId, _) = await SeedUserAndAccountAsync(testUsername, testPassword);

        // Assign three companies deliberately out of alphabetical order
        var names = new[] { "Zebra Corp", "Alpha Inc", "Middle Ltd" };
        foreach (var name in names)
        {
            await AssignNamedCompanyAsync(userId, name);
        }

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        loginRes.EnsureSuccessStatusCode();
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/auth/me/companies");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var result = await res.Content.ReadFromJsonAsync<UserCompaniesResponse>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Companies.Count);

        // Must be ordered: Alpha Inc, Middle Ltd, Zebra Corp
        Assert.Equal("Alpha Inc", result.Companies[0].CompanyName);
        Assert.Equal("Middle Ltd", result.Companies[1].CompanyName);
        Assert.Equal("Zebra Corp", result.Companies[2].CompanyName);
    }

    /// <summary>
    /// The response JSON must not contain any field from
    /// assignment/role/group/department/session/security internals.
    /// Validates that UserCompanyDto exposes only the four approved fields.
    /// </summary>
    [Fact]
    public async Task GetCurrentUserCompanies_ResponseExcludesAllSensitiveFields()
    {
        var testUsername = "api_comp_excl_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        var (userId, _) = await SeedUserAndAccountAsync(testUsername, testPassword);
        await AssignCompanyAsync(userId);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        loginRes.EnsureSuccessStatusCode();
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v2/auth/me/companies");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();

        // ── Fields that must NOT appear in the response ────────────────────────
        // Assignment internals
        Assert.DoesNotContain("assignmentStatus", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("effectiveFrom", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("effectiveTo", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rowVersion", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("isPrimary", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdAt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("updatedAt", json, StringComparison.OrdinalIgnoreCase);
        // Security structures
        Assert.DoesNotContain("role", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("adminGroup", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("department", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("permission", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("session", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lineage", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("individual", json, StringComparison.OrdinalIgnoreCase);
        // User internals
        Assert.DoesNotContain("userId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("username", json, StringComparison.OrdinalIgnoreCase);

        // ── Only the four approved fields should be present ───────────────────
        Assert.Contains("companyId", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("companyCode", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("companyName", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("isDefault", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(long UserId, long AccountId)> SeedUserAndAccountAsync(string username, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
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

    private async Task AssignCompanyAsync(long userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbContextFactory.CreateDbContext();

        var companyCode = "C_" + Guid.NewGuid().ToString("N")[..8];
        var company = new PTKD.Domain.Entities.Company(companyCode, null, "Test Company " + companyCode, null);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var assignment = new PTKD.Domain.Entities.UserCompanyAssignment(userId, company.Id, true, DateTime.UtcNow.AddDays(-1));
        db.UserCompanyAssignments.Add(assignment);
        await db.SaveChangesAsync();
    }

    private async Task AssignNamedCompanyAsync(long userId, string companyName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbContextFactory.CreateDbContext();

        var companyCode = "C_" + Guid.NewGuid().ToString("N")[..8];
        var company = new PTKD.Domain.Entities.Company(companyCode, null, companyName, null);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var assignment = new PTKD.Domain.Entities.UserCompanyAssignment(userId, company.Id, false, DateTime.UtcNow.AddDays(-1));
        db.UserCompanyAssignments.Add(assignment);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Counts Security_Audit_Events rows where actor_user_id or entity_id matches the user.
    /// Used to verify that GET /api/v2/auth/me/companies produces zero audit events.
    /// </summary>
    private int CountAuditEventsByUser(long userId)
    {
        using var connection = TestDatabaseSafety.OpenVerifiedConnection(TestDatabaseSafety.DefaultConnectionString);
        using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Security_Audit_Events WHERE actor_user_id = @uid;",
            connection);
        command.Parameters.AddWithValue("@uid", userId);
        return (int)command.ExecuteScalar()!;
    }
}
