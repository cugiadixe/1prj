using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PTKD.ApiTests;

[Collection("Sequential")]
public class ReconciliationApiTests : IClassFixture<SafeTestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly SafeTestWebApplicationFactory _factory;
    private long _userId;
    private long _companyId;
    private long _customerId;

    public ReconciliationApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var (userId, token) = await SeedUserAndGetTokenAsync("recon_admin");
        _userId = userId;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        (_companyId, _customerId) = await SeedCompanyAndCustomerAsync();

        await GrantPermissionAsync(userId, "RECONCILIATION_PREPARE", _companyId);
        await GrantPermissionAsync(userId, "RECONCILIATION_CONFIRM", _companyId);
        await GrantPermissionAsync(userId, "PAYMENT_CREATE_DRAFT", _companyId);
        await GrantPermissionAsync(userId, "PAYMENT_CONFIRM", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_VIEW", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_CREATE_STANDARD", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_TYPE_MANAGE", null);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDailyReport_Unauthenticated_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/api/v2/reconciliation/daily?companyId=1&date=2026-08-03");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDailyReport_NoPermission_Returns403()
    {
        var (_, token) = await SeedUserAndGetTokenAsync("recon_noperm");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync($"/api/v2/reconciliation/daily?companyId={_companyId}&date=2026-08-03");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDailyReport_Valid_Returns200()
    {
        var response = await _client.GetAsync($"/api/v2/reconciliation/daily?companyId={_companyId}&date={DateTime.UtcNow:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMonthlyReport_Valid_Returns200()
    {
        var now = DateTime.UtcNow;
        var response = await _client.GetAsync($"/api/v2/reconciliation/monthly?companyId={_companyId}&year={now.Year}&month={now.Month}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Prepare_NoPermission_Returns403_AndDoesNotMutateState()
    {
        var periodId = await SeedReconciliationPeriodAsync(_companyId, "DAILY", DateTime.UtcNow.Date);
        var rv = await GetPeriodRowVersionAsync(periodId);

        var (_, token) = await SeedUserAndGetTokenAsync("recon_noprep");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/api/v2/reconciliation/periods/{periodId}/prepare", new { RowVersion = rv });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var statusAfter = await GetPeriodStatusAsync(periodId);
        Assert.Equal("OPEN", statusAfter);
    }

    [Fact]
    public async Task Confirm_NoPermission_Returns403_AndDoesNotMutateState()
    {
        var periodId = await SeedReconciliationPeriodAsync(_companyId, "DAILY", DateTime.UtcNow.Date.AddDays(-1));
        await PreparePeriodDirectlyAsync(periodId);
        var rv = await GetPeriodRowVersionAsync(periodId);

        var (_, token) = await SeedUserAndGetTokenAsync("recon_noconf");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/api/v2/reconciliation/periods/{periodId}/confirm", new { RowVersion = rv });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var statusAfter = await GetPeriodStatusAsync(periodId);
        Assert.Equal("PREPARED", statusAfter);
    }

    [Fact]
    public async Task Prepare_Authorized_Returns200()
    {
        var periodId = await SeedReconciliationPeriodAsync(_companyId, "DAILY", DateTime.UtcNow.Date.AddDays(-2));
        var rv = await GetPeriodRowVersionAsync(periodId);

        var response = await _client.PostAsJsonAsync($"/api/v2/reconciliation/periods/{periodId}/prepare", new { RowVersion = rv });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("PREPARED", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Confirm_Authorized_Returns200()
    {
        var periodId = await SeedReconciliationPeriodAsync(_companyId, "DAILY", DateTime.UtcNow.Date.AddDays(-3));
        await PreparePeriodDirectlyAsync(periodId);
        var rv = await GetPeriodRowVersionAsync(periodId);

        var response = await _client.PostAsJsonAsync($"/api/v2/reconciliation/periods/{periodId}/confirm", new { RowVersion = rv });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("CONFIRMED", doc.RootElement.GetProperty("status").GetString());
    }

    private async Task<long> SeedReconciliationPeriodAsync(long companyId, string periodType, DateTime periodDate)
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var period = PTKD.Domain.Entities.ReconciliationPeriod.Create(companyId, periodType, periodDate);
        db.ReconciliationPeriods.Add(period);
        await db.SaveChangesAsync();
        return period.Id;
    }

    private async Task PreparePeriodDirectlyAsync(long periodId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var period = await db.ReconciliationPeriods.FindAsync(periodId);
        period!.Prepare(_userId, 0, 0);
        await db.SaveChangesAsync();
    }

    private async Task<string> GetPeriodRowVersionAsync(long periodId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var period = await db.ReconciliationPeriods.FindAsync(periodId);
        return Convert.ToBase64String(period!.RowVersion);
    }

    private async Task<string> GetPeriodStatusAsync(long periodId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var period = await db.ReconciliationPeriods.FindAsync(periodId);
        return period!.Status;
    }

    private async Task<(long CompanyId, long CustomerId)> SeedCompanyAndCustomerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var company = new PTKD.Domain.Entities.Company(
            companyCode: "CO_" + Guid.NewGuid().ToString("N")[..6],
            parentCompanyId: null, name: "Recon Test Co", taxCode: null);
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var profile = new PTKD.Domain.Entities.Profile(
            fullName: "Recon Customer", cccd: null, dob: null, dobPartial: null,
            dobPrecision: null, gender: null, permanentAddress: null,
            cccdIssueDate: null, cccdIssuePlace: null, taxCode: null,
            phone: null, contactAddress: null, deathDateSolar: null,
            deathDateLunar: null, deathPlace: null, hometown: null);
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();

        var customer = new PTKD.Domain.Entities.Customer(
            customerCode: "CUS_" + Guid.NewGuid().ToString("N")[..6],
            profileId: profile.Id);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var ctx = new PTKD.Domain.Entities.CustomerCompanyContext(
            customerId: customer.Id, companyId: company.Id,
            assignedStaffId: null, internalNotes: null, firstInteractionAt: null);
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
            employeeCode: username, fullName: "Test User " + username,
            email: null, employmentStatus: "Active", accountStatus: "Active");
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
                PermissionCode = permissionCode, ModuleCode = "TEST", ActionCode = "TEST",
                DataScope = companyId.HasValue ? "COMPANY" : "GLOBAL",
                IsSensitive = false, RequiresReason = false, IsDelegable = false,
                IsActive = true, CreatedAt = DateTime.UtcNow,
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
            UserId = userId, PermissionCode = permissionCode,
            ScopeType = companyId.HasValue ? "COMPANY" : "GLOBAL",
            CompanyId = companyId, GrantType = "ALLOW", AssignmentStatus = "ACTIVE",
            EffectiveFrom = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            RowVersion = PTKD.Domain.ValueObjects.RowVersion.FromByteArray(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 })
        };
        db.Set<PTKD.Domain.Security.Authorization.UserIndividualPermission>().Add(up);
        await db.SaveChangesAsync();
    }
}
