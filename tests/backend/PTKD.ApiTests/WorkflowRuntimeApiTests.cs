using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace PTKD.ApiTests;

public class WorkflowRuntimeApiTests : IClassFixture<SafeTestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly SafeTestWebApplicationFactory _factory;
    private long _userId;

    public WorkflowRuntimeApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var (userId, token) = await SeedUserAndGetTokenAsync("wf_requester");
        _userId = userId;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

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

        var loginReq = new PTKD.Api.Auth.Models.LoginRequest(username, password);
        var authClient = _factory.CreateClient();
        var loginRes = await authClient.PostAsJsonAsync("/api/v2/auth/login", loginReq);
        loginRes.EnsureSuccessStatusCode();

        var body = await loginRes.Content.ReadFromJsonAsync<PTKD.Api.Auth.Models.LoginResponse>();
        return (user.Id, body!.AccessToken);
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

        var exists = db.Set<PTKD.Domain.Security.Authorization.UserIndividualPermission>()
            .Any(p => p.UserId == userId && p.PermissionCode == permissionCode);
        if (exists) return;

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

    // ── Tests ────────────────────────────────────────

    [Fact]
    public async Task GetMyRequests_Returns200()
    {
        var response = await _client.GetAsync("/api/v2/workflows/my-requests");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Status: {response.StatusCode}, Body: {body}");
    }

    [Fact]
    public async Task GetInstanceActions_UnauthorizedUser_Returns403Or404()
    {
        // Try getting actions for a non-existent instance
        var response = await _client.GetAsync("/api/v2/workflows/instances/9999/actions");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden, $"Status: {response.StatusCode}, Body: {body}");
    }

    [Fact]
    public async Task RetryExecution_WithoutPermission_Returns403()
    {
        var response = await _client.PostAsync("/api/v2/workflows/instances/1/retry-execution", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RetryExecution_WithPermission_ForNotFoundInstance_Returns404()
    {
        var (uid, token) = await SeedUserAndGetTokenAsync("wf_admin");
        await GrantPermissionAsync(uid, "WORKFLOW_RETRY_EXECUTION", null);

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await adminClient.PostAsync("/api/v2/workflows/instances/9999/retry-execution", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
