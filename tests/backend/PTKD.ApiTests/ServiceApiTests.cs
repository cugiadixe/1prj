using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PTKD.ApiTests;

public class ServiceApiTests : IClassFixture<SafeTestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly SafeTestWebApplicationFactory _factory;
    private long _userId;
    private long _companyId;
    private long _customerId;

    public ServiceApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var (userId, token) = await SeedUserAndGetTokenAsync("svc_admin");
        _userId = userId;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        (_companyId, _customerId) = await SeedCompanyAndCustomerAsync();

        await GrantPermissionAsync(userId, "SERVICE_VIEW", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_CREATE_STANDARD", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_RENEW_STANDARD", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_PRICE_OVERRIDE_REQUEST", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_TYPE_MANAGE", null);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ListServices_Unauthenticated_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/api/v2/services?companyId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListServices_NoPermission_Returns403()
    {
        var (_, token) = await SeedUserAndGetTokenAsync("svc_no_view");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync($"/api/v2/services?companyId={_companyId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateService_Valid_Returns201()
    {
        var serviceTypeId = await CreateServiceTypeAsync();

        var request = new
        {
            ServiceTypeId = serviceTypeId,
            CustomerId = _customerId,
            CompanyId = _companyId,
            ValidFrom = DateTime.UtcNow
        };

        var response = await _client.PostAsJsonAsync("/api/v2/services", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("ACTIVE", doc.RootElement.GetProperty("status").GetString());
        Assert.True(doc.RootElement.GetProperty("appliedPrice").GetDecimal() > 0);
    }

    [Fact]
    public async Task CreateService_InvalidCustomer_Returns400()
    {
        var serviceTypeId = await CreateServiceTypeAsync();
        var request = new
        {
            ServiceTypeId = serviceTypeId,
            CustomerId = 999999L,
            CompanyId = _companyId,
            ValidFrom = DateTime.UtcNow
        };
        var response = await _client.PostAsJsonAsync("/api/v2/services", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetService_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/v2/services/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RenewService_Valid_Returns201()
    {
        var serviceTypeId = await CreateServiceTypeAsync();
        var svc = await CreateServiceAsync(serviceTypeId);

        var renewRequest = new { ValidFrom = DateTime.UtcNow.AddMonths(12), RowVersion = svc.RowVersion };
        var response = await _client.PostAsJsonAsync($"/api/v2/services/{svc.Id}/renew", renewRequest);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(2, doc.RootElement.GetProperty("cycleNumber").GetInt32());
    }

    private record ServiceResponse(long Id, string Status, string RowVersion);

    private async Task<long> CreateServiceTypeAsync()
    {
        var code = "ST_" + Guid.NewGuid().ToString("N")[..8];
        var request = new { Code = code, Name = "Test Type", StandardPrice = 50_000m };
        var response = await _client.PostAsJsonAsync("/api/v2/service-types", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetInt64();
    }

    private async Task<ServiceResponse> CreateServiceAsync(long serviceTypeId)
    {
        var request = new
        {
            ServiceTypeId = serviceTypeId,
            CustomerId = _customerId,
            CompanyId = _companyId,
            ValidFrom = DateTime.UtcNow
        };
        var response = await _client.PostAsJsonAsync("/api/v2/services", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return new ServiceResponse(
            doc.RootElement.GetProperty("id").GetInt64(),
            doc.RootElement.GetProperty("status").GetString()!,
            doc.RootElement.GetProperty("rowVersion").GetString()!);
    }

    private async Task<(long CompanyId, long CustomerId)> SeedCompanyAndCustomerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var company = new PTKD.Domain.Entities.Company(
            companyCode: "CO_" + Guid.NewGuid().ToString("N")[..6],
            parentCompanyId: null,
            name: "Test Company",
            taxCode: null);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var profile = new PTKD.Domain.Entities.Profile(
            fullName: "Test Customer",
            cccd: null,
            dob: null,
            dobPartial: null,
            dobPrecision: null,
            gender: null,
            permanentAddress: null,
            cccdIssueDate: null,
            cccdIssuePlace: null,
            taxCode: null,
            phone: null,
            contactAddress: null,
            deathDateSolar: null,
            deathDateLunar: null,
            deathPlace: null,
            hometown: null);
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        var customer = new PTKD.Domain.Entities.Customer(
            customerCode: "CUS_" + Guid.NewGuid().ToString("N")[..6],
            profileId: profile.Id);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var ctx = new PTKD.Domain.Entities.CustomerCompanyContext(
            customerId: customer.Id,
            companyId: company.Id,
            assignedStaffId: null,
            internalNotes: null,
            firstInteractionAt: null);
        db.CustomerCompanyContexts.Add(ctx);
        await db.SaveChangesAsync();

        return (company.Id, customer.Id);
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

        var loginBody = await loginRes.Content.ReadFromJsonAsync<PTKD.Api.Auth.Models.LoginResponse>();
        return (user.Id, loginBody!.AccessToken);
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
