using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PTKD.ApiTests;

public class CustomerApiTests : IClassFixture<SafeTestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly SafeTestWebApplicationFactory _factory;
    private long _userId;

    public CustomerApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var (userId, token) = await SeedUserAndGetTokenAsync("cust_admin");
        _userId = userId;
        await GrantPermissionAsync(userId, "CUSTOMER_VIEW_BASIC", null);
        await GrantPermissionAsync(userId, "CUSTOMER_VIEW_SENSITIVE", null);
        await GrantPermissionAsync(userId, "CUSTOMER_CREATE_FINAL", null);
        await GrantPermissionAsync(userId, "CUSTOMER_MASTER_UPDATE", null);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Authorization ────────────────────────────────────────

    [Fact]
    public async Task Customers_Unauthenticated_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/api/v2/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Customers_NoPermission_Returns403()
    {
        var (_, token) = await SeedUserAndGetTokenAsync("cust_noperm");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v2/customers");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Customer CRUD ────────────────────────────────────────

    [Fact]
    public async Task Customer_Create_Valid_Returns201()
    {
        var request = MakeCreateRequest();
        var response = await _client.PostAsJsonAsync("/api/v2/customers", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("id", out _));
        Assert.True(doc.RootElement.TryGetProperty("rowVersion", out _));
        Assert.True(doc.RootElement.TryGetProperty("profile", out _));
    }

    [Fact]
    public async Task Customer_GetById_Returns200()
    {
        var created = await CreateCustomerAsync();
        var response = await _client.GetAsync($"/api/v2/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Customer_GetById_Missing_Returns404()
    {
        var response = await _client.GetAsync("/api/v2/customers/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Customer_Search_Returns200()
    {
        await CreateCustomerAsync();
        var response = await _client.GetAsync("/api/v2/customers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("items", out _));
        Assert.True(doc.RootElement.TryGetProperty("totalCount", out _));
    }

    [Fact]
    public async Task Customer_Update_Valid_Returns200()
    {
        var created = await CreateCustomerAsync();
        var update = new
        {
            FullName = "Updated Name",
            Reason = "Test correction",
            TargetVersion = created.RowVersion
        };
        var response = await _client.PutAsJsonAsync($"/api/v2/customers/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Customer_Update_StaleRowVersion_Returns409()
    {
        var created = await CreateCustomerAsync();

        var update1 = new { FullName = "First Update", Reason = "First", TargetVersion = created.RowVersion };
        await _client.PutAsJsonAsync($"/api/v2/customers/{created.Id}", update1);

        var update2 = new { FullName = "Stale Update", Reason = "Stale", TargetVersion = created.RowVersion };
        var response = await _client.PutAsJsonAsync($"/api/v2/customers/{created.Id}", update2);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("CUS_INVALID_ROW_VERSION", content);
    }

    [Fact]
    public async Task Customer_Update_RequiresReason_Returns400()
    {
        var created = await CreateCustomerAsync();
        var update = new { FullName = "Updated", Reason = "", TargetVersion = created.RowVersion };
        var response = await _client.PutAsJsonAsync($"/api/v2/customers/{created.Id}", update);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Duplicate Detection ────────────────────────────────────────

    [Fact]
    public async Task Customer_DuplicateCode_Returns400()
    {
        var created = await CreateCustomerAsync();
        var request = MakeCreateRequest();
        request.CustomerCode = created.CustomerCode;
        var response = await _client.PostAsJsonAsync("/api/v2/customers", request);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("CUS_DUPLICATE_CUSTOMER_CODE", content);
    }

    [Fact]
    public async Task Customer_DuplicateCccd_Returns400()
    {
        var cccd = "CCCD_" + Guid.NewGuid().ToString("N")[..10];
        var request1 = MakeCreateRequest();
        request1.Cccd = cccd;
        await _client.PostAsJsonAsync("/api/v2/customers", request1);

        var request2 = MakeCreateRequest();
        request2.Cccd = cccd;
        var response = await _client.PostAsJsonAsync("/api/v2/customers", request2);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("CUS_DUPLICATE_CCCD", content);
    }

    [Fact]
    public async Task Customer_DuplicateCheck_Returns200()
    {
        var cccd = "DC_" + Guid.NewGuid().ToString("N")[..10];
        var request = MakeCreateRequest();
        request.Cccd = cccd;
        await _client.PostAsJsonAsync("/api/v2/customers", request);

        var response = await _client.GetAsync($"/api/v2/customers/duplicate-check?cccd={cccd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("hasDuplicates").GetBoolean());
    }

    // ── Company Contexts ────────────────────────────────────────

    [Fact]
    public async Task Customer_CompanyContexts_Returns200()
    {
        var created = await CreateCustomerAsync();
        var response = await _client.GetAsync($"/api/v2/customers/{created.Id}/company-contexts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CreateCompanyContext_Returns201()
    {
        var created = await CreateCustomerWithCompanyAsync();
        var company = await CreateCompanyAsync();
        var request = new { CompanyId = company.Id };
        var response = await _client.PostAsJsonAsync($"/api/v2/customers/{created.Id}/company-contexts", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Customer_DuplicateCompanyContext_Rejected()
    {
        var created = await CreateCustomerWithCompanyAsync();
        var contexts = await GetCompanyContextsAsync(created.Id);
        if (contexts.Length > 0)
        {
            var request = new { CompanyId = contexts[0].CompanyId };
            var response = await _client.PostAsJsonAsync($"/api/v2/customers/{created.Id}/company-contexts", request);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict);
        }
    }

    // ── Validation ────────────────────────────────────────

    [Fact]
    public async Task Customer_Create_EmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v2/customers", new
        {
            CustomerCode = "TEST",
            FullName = ""
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Customer_Create_EmptyCode_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v2/customers", new
        {
            CustomerCode = "",
            FullName = "Test"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Sensitive Data Masking ────────────────────────────────

    [Fact]
    public async Task Customer_SensitiveFieldsMasked_WhenNoSensitivePermission()
    {
        var cccd = "123456789012";
        var phone = "0901234567";
        var request = MakeCreateRequest();
        request.Cccd = cccd;
        request.Phone = phone;
        request.PermanentAddress = "123 Secret Street";
        var createResp = await _client.PostAsJsonAsync("/api/v2/customers", request);
        createResp.EnsureSuccessStatusCode();
        var created = await ParseCustomerAsync(createResp);

        // Create a user WITHOUT CUSTOMER_VIEW_SENSITIVE
        var (userId2, token2) = await SeedUserAndGetTokenAsync("cust_basic_only");
        await GrantPermissionAsync(userId2, "CUSTOMER_VIEW_BASIC", null);
        var basicClient = _factory.CreateClient();
        basicClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);

        var response = await basicClient.GetAsync($"/api/v2/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var profile = doc.RootElement.GetProperty("profile");
        var maskedCccd = profile.GetProperty("cccd").GetString();
        Assert.NotEqual(cccd, maskedCccd);
        Assert.EndsWith("9012", maskedCccd!);
    }

    // ── Helpers ────────────────────────────────────────────────

    private record CustomerResponse(long Id, string CustomerCode, string RowVersion);
    private record ContextResponse(long Id, long CompanyId, string RowVersion);

    private static object MakeCreateRequestObj()
    {
        var code = "CUS_" + Guid.NewGuid().ToString("N")[..10];
        return new
        {
            CustomerCode = code,
            FullName = "Customer " + code,
        };
    }

    private static PTKD.Application.Customers.DTOs.CreateCustomerRequest MakeCreateRequest()
    {
        var code = "CUS_" + Guid.NewGuid().ToString("N")[..10];
        return new PTKD.Application.Customers.DTOs.CreateCustomerRequest
        {
            CustomerCode = code,
            FullName = "Customer " + code
        };
    }

    private async Task<CustomerResponse> CreateCustomerAsync()
    {
        var request = MakeCreateRequestObj();
        var response = await _client.PostAsJsonAsync("/api/v2/customers", request);
        response.EnsureSuccessStatusCode();
        return await ParseCustomerAsync(response);
    }

    private async Task<CustomerResponse> CreateCustomerWithCompanyAsync()
    {
        var company = await CreateCompanyAsync();
        var code = "CUS_" + Guid.NewGuid().ToString("N")[..10];
        var request = new
        {
            CustomerCode = code,
            FullName = "Customer " + code,
            InitialCompanyId = company.Id
        };
        var response = await _client.PostAsJsonAsync("/api/v2/customers", request);
        response.EnsureSuccessStatusCode();
        return await ParseCustomerAsync(response);
    }

    private async Task<CustomerResponse> CreateCompanyAsync()
    {
        await GrantPermissionAsync(_userId, "ORGANIZATION_COMPANY_MANAGE", null);
        var code = "C_" + Guid.NewGuid().ToString("N")[..10];
        var response = await _client.PostAsJsonAsync("/api/v2/organizations/companies",
            new { CompanyCode = code, Name = "Company " + code });
        response.EnsureSuccessStatusCode();
        return await ParseCustomerAsync(response);
    }

    private async Task<ContextResponse[]> GetCompanyContextsAsync(long customerId)
    {
        var response = await _client.GetAsync($"/api/v2/customers/{customerId}/company-contexts");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var arr = doc.RootElement.EnumerateArray();
        var list = new System.Collections.Generic.List<ContextResponse>();
        foreach (var elem in arr)
        {
            list.Add(new ContextResponse(
                elem.GetProperty("id").GetInt64(),
                elem.GetProperty("companyId").GetInt64(),
                elem.GetProperty("rowVersion").GetString() ?? ""));
        }
        return list.ToArray();
    }

    private static async Task<CustomerResponse> ParseCustomerAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        return new CustomerResponse(
            root.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
            root.TryGetProperty("customerCode", out var cc) ? cc.GetString() ?? "" : "",
            root.TryGetProperty("rowVersion", out var rv) ? rv.GetString() ?? "" : "");
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
