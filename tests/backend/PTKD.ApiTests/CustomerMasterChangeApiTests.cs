using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PTKD.ApiTests;

public class CustomerMasterChangeApiTests : IClassFixture<SafeTestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly SafeTestWebApplicationFactory _factory;
    private long _userId;

    public CustomerMasterChangeApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await SeedWorkflowAsync();
        var (userId, token) = await SeedUserAndGetTokenAsync("cmc_admin");
        _userId = userId;
        await GrantPermissionAsync(userId, "CUSTOMER_CHANGE_REQUEST_CREATE", null);
        await GrantPermissionAsync(userId, "CUSTOMER_CREATE_FINAL", null);
        await GrantPermissionAsync(userId, "CUSTOMER_VIEW_BASIC", null);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task SeedWorkflowAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE id = 1)
            BEGIN
                SET IDENTITY_INSERT dbo.Users ON;
                INSERT INTO dbo.Users (id, employee_code, full_name, email, employment_status, account_status, created_at)
                VALUES (1, 'SYS', 'System', 'sys@ptkd.local', 'ACTIVE', 'ACTIVE', GETUTCDATE());
                SET IDENTITY_INSERT dbo.Users OFF;
            END

            IF NOT EXISTS (SELECT 1 FROM dbo.Business_Process_Catalog WHERE process_code = 'CUSTOMER_MASTER_CHANGE')
                INSERT INTO dbo.Business_Process_Catalog (process_code, process_name, is_approval_required, is_active, created_at)
                VALUES ('CUSTOMER_MASTER_CHANGE', 'Customer Master Change', 1, 1, GETUTCDATE());
            
            IF NOT EXISTS (SELECT 1 FROM dbo.Workflow_Definitions WHERE process_code = 'CUSTOMER_MASTER_CHANGE')
            BEGIN
                INSERT INTO dbo.Workflow_Definitions (definition_code, definition_name, process_code, is_active, created_by, created_at)
                VALUES ('CMC_DEF_1', 'CMC Def', 'CUSTOMER_MASTER_CHANGE', 1, 1, GETUTCDATE());
                DECLARE @defId BIGINT = SCOPE_IDENTITY();
                
                INSERT INTO dbo.Workflow_Definition_Versions (workflow_definition_id, version_number, version_status, created_by, created_at)
                VALUES (@defId, 1, 'ACTIVE', 1, GETUTCDATE());
                DECLARE @verId BIGINT = SCOPE_IDENTITY();

                INSERT INTO dbo.Workflow_Bindings (process_code, workflow_version_id, scope_type, priority, effective_from, is_active, created_by, created_at)
                VALUES ('CUSTOMER_MASTER_CHANGE', @verId, 'GLOBAL', 1, GETUTCDATE(), 1, 1, GETUTCDATE());
            END
        ";
        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, sql);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateChangeRequest_Success_Returns200()
    {
        var customer = await CreateActiveCustomerAsync();
        var request = new
        {
            TargetCustomerId = customer.Id,
            TargetRowVersion = customer.RowVersion,
            FullName = "Updated Master Name",
            Reason = "Valid Reason"
        };
        
        var response = await _client.PostAsJsonAsync($"/api/v2/customers/{customer.Id}/change-requests", request);
        var bodyStr = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) throw new Exception($"Status {response.StatusCode}, Body: {bodyStr}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("id", out _));
        Assert.True(doc.RootElement.TryGetProperty("workflowInstanceId", out _));
        Assert.Equal("CUSTOMER_MASTER_CHANGE", doc.RootElement.GetProperty("processCode").GetString());
    }

    [Fact]
    public async Task CreateChangeRequest_Unauthenticated_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.PostAsJsonAsync("/api/v2/customers/1/change-requests", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateChangeRequest_NoPermission_Returns403()
    {
        var (_, token) = await SeedUserAndGetTokenAsync("cmc_noperm");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsJsonAsync("/api/v2/customers/1/change-requests", new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateChangeRequest_MismatchedId_Returns400()
    {
        var request = new
        {
            TargetCustomerId = 2,
            TargetRowVersion = Convert.ToBase64String(new byte[] { 1 }),
            Reason = "Mismatched"
        };
        
        var response = await _client.PostAsJsonAsync("/api/v2/customers/1/change-requests", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyChangeRequests_Success_Returns200()
    {
        var customer = await CreateActiveCustomerAsync();
        var request = new
        {
            TargetCustomerId = customer.Id,
            TargetRowVersion = customer.RowVersion,
            FullName = "Name",
            Reason = "My Request"
        };
        await _client.PostAsJsonAsync($"/api/v2/customers/{customer.Id}/change-requests", request);

        var response = await _client.GetAsync("/api/v2/customers/my-change-requests");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.EnumerateArray();
        Assert.Contains(items, i => i.GetProperty("processCode").GetString() == "CUSTOMER_MASTER_CHANGE");
    }

    [Fact]
    public async Task GetChangeRequestById_Success_Returns200()
    {
        var customer = await CreateActiveCustomerAsync();
        var request = new
        {
            TargetCustomerId = customer.Id,
            TargetRowVersion = customer.RowVersion,
            FullName = "Name 2",
            Reason = "My Request 2"
        };
        var createRes = await _client.PostAsJsonAsync($"/api/v2/customers/{customer.Id}/change-requests", request);
        var createBody = await createRes.Content.ReadAsStringAsync();
        var createdId = JsonDocument.Parse(createBody).RootElement.GetProperty("id").GetInt64();

        var response = await _client.GetAsync($"/api/v2/customers/change-requests/{createdId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Assert no stack trace or raw SQL in body by ensuring it matches safe DTO
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("id", out _));
        Assert.False(doc.RootElement.TryGetProperty("stackTrace", out _));
    }

    private async Task<(long Id, string RowVersion)> CreateActiveCustomerAsync()
    {
        var request = new
        {
            CustomerCode = "CUS_" + Guid.NewGuid().ToString("N")[..10],
            FullName = "Test Customer " + Guid.NewGuid().ToString("N")[..5]
        };
        var response = await _client.PostAsJsonAsync("/api/v2/customers", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return (doc.RootElement.GetProperty("id").GetInt64(), doc.RootElement.GetProperty("rowVersion").GetString()!);
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
}
