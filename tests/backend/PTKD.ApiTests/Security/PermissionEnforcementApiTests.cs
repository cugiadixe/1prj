using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Api.Auth.Models;
using PTKD.IntegrationTests;

namespace PTKD.ApiTests.Security;

[Collection("Sequential")]
public class PermissionEnforcementApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PermissionEnforcementApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCompanyScoped_WithoutJwt_Returns401()
    {
        var response = await _client.GetAsync("/api/v2/test/permissiontest/company-scoped");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCompanyScoped_WithJwt_MissingCompanyId_Returns400()
    {
        var (_, token) = await SeedUserAndGetTokenAsync("api_user_no_comp");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v2/test/permissiontest/company-scoped");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = problem.GetProperty("type").GetString();
        Assert.Equal("https://ptkd-erp.example.com/errors/missing-company-context", type);
    }

    [Fact]
    public async Task GetCompanyScoped_WithJwt_MalformedCompanyId_Returns400()
    {
        var (_, token) = await SeedUserAndGetTokenAsync("api_user_mal_comp");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Add("X-Company-Id", "NOT_A_NUMBER");

        var response = await _client.GetAsync("/api/v2/test/permissiontest/company-scoped");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = problem.GetProperty("type").GetString();
        Assert.Equal("https://ptkd-erp.example.com/errors/malformed-company-context", type);
    }

    [Fact]
    public async Task GetCompanyScoped_WithJwt_NoActiveCompanyAssignment_Returns403()
    {
        var (_, token) = await SeedUserAndGetTokenAsync("api_user_no_assign");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Add("X-Company-Id", "999"); // No assignment

        var response = await _client.GetAsync("/api/v2/test/permissiontest/company-scoped");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCompanyScoped_WithJwt_ActiveAssignment_MissingPermission_Returns403()
    {
        var (userId, token) = await SeedUserAndGetTokenAsync("api_user_no_perm");
        var companyId = await SeedCompanyAndAssignUserAsync(userId);
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Add("X-Company-Id", companyId.ToString());

        var response = await _client.GetAsync("/api/v2/test/permissiontest/company-scoped");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCompanyScoped_WithJwt_ActiveAssignment_WithPermission_Returns200()
    {
        var (userId, token) = await SeedUserAndGetTokenAsync("api_user_has_perm");
        var companyId = await SeedCompanyAndAssignUserAsync(userId);
        await GrantPermissionAsync(userId, "TEST_COMPANY_PERM", companyId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Add("X-Company-Id", companyId.ToString());

        var response = await _client.GetAsync("/api/v2/test/permissiontest/company-scoped");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGlobalScoped_WithJwt_WithoutCompanyId_WithPermission_Returns200()
    {
        var (userId, token) = await SeedUserAndGetTokenAsync("api_user_global_perm");
        await GrantPermissionAsync(userId, "TEST_GLOBAL_PERM", null); // Global grant

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v2/test/permissiontest/global-scoped");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGlobalScoped_WithJwt_WithCompanyId_WithPermission_Returns200AndIgnoresHeader()
    {
        var (userId, token) = await SeedUserAndGetTokenAsync("api_user_global_perm2");
        await GrantPermissionAsync(userId, "TEST_GLOBAL_PERM", null); // Global grant

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Add("X-Company-Id", "not_a_valid_company_but_ignored");

        var response = await _client.GetAsync("/api/v2/test/permissiontest/global-scoped");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthEndpoints_AreNotAffectedByCompanyEnforcement()
    {
        // Ping login endpoint with missing body/invalid body -> should return 401/400 from auth logic, NOT missing company context
        var request = new LoginRequest("fake", "fake");
        var response = await _client.PostAsJsonAsync("/api/v2/auth/login", request);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = problem.GetProperty("type").GetString();
        Assert.Equal("https://ptkd-erp.internal/docs/errors/auth/invalid-credentials", type);
    }

    // --- Helpers ---

    private async Task<(long UserId, string Token)> SeedUserAndGetTokenAsync(string baseUsername)
    {
        var username = baseUsername + "_" + Guid.NewGuid().ToString("N")[..8];
        var password = "ValidPassword123!";

        using var scope = _factory.Services.CreateScope();
        var clock = scope.ServiceProvider.GetRequiredService<PTKD.Application.Security.Authentication.Interfaces.IUtcClock>();
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<PTKD.Domain.Entities.UserAuthAccount>();

        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var user = new PTKD.Domain.Entities.User(username, "Test", username + "@example.com", "ACTIVE", "ACTIVE");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var dummyAccount = PTKD.Domain.Entities.UserAuthAccount.CreateInternal(user.Id, username.ToUpperInvariant(), "TEMP", clock.UtcNow);
        var hash = hasher.HashPassword(dummyAccount, password);
        var account = PTKD.Domain.Entities.UserAuthAccount.CreateInternal(user.Id, username.ToUpperInvariant(), hash, clock.UtcNow);
        db.UserAuthAccounts.Add(account);
        await db.SaveChangesAsync();

        var loginReq = new LoginRequest(username, password);
        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", loginReq);
        loginRes.EnsureSuccessStatusCode();

        var body = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        return (user.Id, body!.AccessToken);
    }

    private async Task<long> SeedCompanyAndAssignUserAsync(long userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var code = "C" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var company = new PTKD.Domain.Entities.Company(code, null, "Test Company " + code, null);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var assignment = new PTKD.Domain.Entities.UserCompanyAssignment(
            userId, company.Id, false, DateTime.UtcNow);
        db.UserCompanyAssignments.Add(assignment);
        await db.SaveChangesAsync();

        return company.Id;
    }

    private async Task GrantPermissionAsync(long userId, string permissionCode, long? companyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var perm = db.Set<PTKD.Domain.Security.Authorization.Permission>().FirstOrDefault(p => p.PermissionCode == permissionCode);
        if (perm == null)
        {
            perm = new PTKD.Domain.Security.Authorization.Permission
            {
                PermissionCode = permissionCode,
                ModuleCode = "TEST",
                ActionCode = "TEST",
                DataScope = companyId.HasValue ? "COMPANY" : "GLOBAL",
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
            ScopeType = companyId.HasValue ? "COMPANY" : "GLOBAL",
            CompanyId = companyId,
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
