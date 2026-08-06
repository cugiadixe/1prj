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
public class PaymentTransactionApiTests : IClassFixture<SafeTestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly SafeTestWebApplicationFactory _factory;
    private long _userId;
    private long _companyId;
    private long _customerId;
    private long _serviceTypeId;
    private long _serviceId;

    public PaymentTransactionApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var (userId, token) = await SeedUserAndGetTokenAsync("pay_admin");
        _userId = userId;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        (_companyId, _customerId) = await SeedCompanyAndCustomerAsync();

        await GrantPermissionAsync(userId, "PAYMENT_CREATE_DRAFT", _companyId);
        await GrantPermissionAsync(userId, "PAYMENT_CONFIRM", _companyId);
        await GrantPermissionAsync(userId, "PAYMENT_CORRECT_CONFIRMED", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_VIEW", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_CREATE_STANDARD", _companyId);
        await GrantPermissionAsync(userId, "SERVICE_TYPE_MANAGE", null);

        (_serviceTypeId, _serviceId) = await SeedServiceTypeAndServiceAsync(_companyId, _customerId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ListPayments_Unauthenticated_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/api/v2/payments?companyId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListPayments_NoPermission_Returns403()
    {
        var (_, token) = await SeedUserAndGetTokenAsync("pay_noperm");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync($"/api/v2/payments?companyId={_companyId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateDraft_Valid_Returns201()
    {
        var request = new
        {
            CustomerId = _customerId,
            CompanyId = _companyId,
            PaymentMethod = "CASH",
            PaymentDate = DateTime.UtcNow,
            Items = new[] { new { ServiceId = _serviceId, Amount = 50_000m } }
        };

        var response = await _client.PostAsJsonAsync("/api/v2/payments", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("DRAFT", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(50_000m, doc.RootElement.GetProperty("totalAmount").GetDecimal());
        Assert.StartsWith("PAY-", doc.RootElement.GetProperty("billCode").GetString());
    }

    [Fact]
    public async Task CreateDraft_NoItems_Returns400()
    {
        var request = new
        {
            CustomerId = _customerId,
            CompanyId = _companyId,
            PaymentMethod = "CASH",
            PaymentDate = DateTime.UtcNow,
            Items = Array.Empty<object>()
        };

        var response = await _client.PostAsJsonAsync("/api/v2/payments", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmPayment_Valid_ReturnsConfirmed()
    {
        var draft = await CreateDraftPaymentAsync();

        var confirmReq = new { RowVersion = draft.RowVersion };
        var response = await _client.PostAsJsonAsync($"/api/v2/payments/{draft.Id}/confirm", confirmReq);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("CONFIRMED", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ConfirmPayment_AlreadyConfirmed_Returns400()
    {
        var draft = await CreateDraftPaymentAsync();
        var confirmReq = new { RowVersion = draft.RowVersion };
        var firstResponse = await _client.PostAsJsonAsync($"/api/v2/payments/{draft.Id}/confirm", confirmReq);
        firstResponse.EnsureSuccessStatusCode();

        var body = await firstResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var newRv = doc.RootElement.GetProperty("rowVersion").GetString()!;

        var secondResponse = await _client.PostAsJsonAsync($"/api/v2/payments/{draft.Id}/confirm", new { RowVersion = newRv });
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetPayment_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/v2/payments/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SoftDeleteDraft_Valid_Returns204()
    {
        var draft = await CreateDraftPaymentAsync();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v2/payments/{draft.Id}");
        request.Content = JsonContent.Create(new { RowVersion = draft.RowVersion });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SoftDeleteConfirmed_Returns400()
    {
        var draft = await CreateDraftPaymentAsync();
        var confirmRes = await _client.PostAsJsonAsync($"/api/v2/payments/{draft.Id}/confirm", new { RowVersion = draft.RowVersion });
        confirmRes.EnsureSuccessStatusCode();
        var confirmBody = await confirmRes.Content.ReadAsStringAsync();
        using var confirmDoc = JsonDocument.Parse(confirmBody);
        var confirmedRv = confirmDoc.RootElement.GetProperty("rowVersion").GetString()!;

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v2/payments/{draft.Id}");
        request.Content = JsonContent.Create(new { RowVersion = confirmedRv });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CorrectConfirmed_Valid_Returns200()
    {
        var draft = await CreateDraftPaymentAsync();
        var confirmRes = await _client.PostAsJsonAsync($"/api/v2/payments/{draft.Id}/confirm", new { RowVersion = draft.RowVersion });
        confirmRes.EnsureSuccessStatusCode();
        var confirmBody = await confirmRes.Content.ReadAsStringAsync();
        using var confirmDoc = JsonDocument.Parse(confirmBody);
        var confirmedRv = confirmDoc.RootElement.GetProperty("rowVersion").GetString()!;

        var correctReq = new
        {
            PaymentMethod = "TRANSFER",
            Reason = "Customer requested transfer instead of cash",
            RowVersion = confirmedRv
        };
        var response = await _client.PostAsJsonAsync($"/api/v2/payments/{draft.Id}/correct", correctReq);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var corrBody = await response.Content.ReadAsStringAsync();
        using var corrDoc = JsonDocument.Parse(corrBody);
        Assert.Equal("TRANSFER", corrDoc.RootElement.GetProperty("paymentMethod").GetString());
    }

    private record PaymentResponse(long Id, string Status, string RowVersion);

    private async Task<PaymentResponse> CreateDraftPaymentAsync()
    {
        var svcId = await CreateNewServiceAsync();
        var request = new
        {
            CustomerId = _customerId,
            CompanyId = _companyId,
            PaymentMethod = "CASH",
            PaymentDate = DateTime.UtcNow,
            Items = new[] { new { ServiceId = svcId, Amount = 50_000m } }
        };
        var response = await _client.PostAsJsonAsync("/api/v2/payments", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return new PaymentResponse(
            doc.RootElement.GetProperty("id").GetInt64(),
            doc.RootElement.GetProperty("status").GetString()!,
            doc.RootElement.GetProperty("rowVersion").GetString()!);
    }

    private async Task<long> CreateNewServiceAsync()
    {
        var code = "ST_" + Guid.NewGuid().ToString("N")[..8];
        var stReq = new { Code = code, Name = "Test Type", StandardPrice = 50_000m };
        var stRes = await _client.PostAsJsonAsync("/api/v2/service-types", stReq);
        stRes.EnsureSuccessStatusCode();
        var stBody = await stRes.Content.ReadAsStringAsync();
        using var stDoc = JsonDocument.Parse(stBody);
        var stId = stDoc.RootElement.GetProperty("id").GetInt64();

        var svcReq = new
        {
            ServiceTypeId = stId,
            CustomerId = _customerId,
            CompanyId = _companyId,
            ValidFrom = DateTime.UtcNow
        };
        var svcRes = await _client.PostAsJsonAsync("/api/v2/services", svcReq);
        svcRes.EnsureSuccessStatusCode();
        var svcBody = await svcRes.Content.ReadAsStringAsync();
        using var svcDoc = JsonDocument.Parse(svcBody);
        return svcDoc.RootElement.GetProperty("id").GetInt64();
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
            cccd: null, dob: null, dobPartial: null, dobPrecision: null,
            gender: null, permanentAddress: null, cccdIssueDate: null,
            cccdIssuePlace: null, taxCode: null, phone: null,
            contactAddress: null, deathDateSolar: null, deathDateLunar: null,
            deathPlace: null, hometown: null);
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

    private async Task<(long ServiceTypeId, long ServiceId)> SeedServiceTypeAndServiceAsync(long companyId, long customerId)
    {
        var code = "ST_" + Guid.NewGuid().ToString("N")[..8];
        var stReq = new { Code = code, Name = "Init Type", StandardPrice = 50_000m };
        var stRes = await _client.PostAsJsonAsync("/api/v2/service-types", stReq);
        var stBody = await stRes.Content.ReadAsStringAsync();
        if (!stRes.IsSuccessStatusCode) throw new Exception($"SeedServiceType failed: {stRes.StatusCode} - {stBody}");
        using var stDoc = JsonDocument.Parse(stBody);
        var stId = stDoc.RootElement.GetProperty("id").GetInt64();

        var svcReq = new { ServiceTypeId = stId, CustomerId = customerId, CompanyId = companyId, ValidFrom = DateTime.UtcNow };
        var svcRes = await _client.PostAsJsonAsync("/api/v2/services", svcReq);
        var svcBody = await svcRes.Content.ReadAsStringAsync();
        if (!svcRes.IsSuccessStatusCode) throw new Exception($"SeedService failed: {svcRes.StatusCode} - {svcBody}");
        using var svcDoc = JsonDocument.Parse(svcBody);
        return (stId, svcDoc.RootElement.GetProperty("id").GetInt64());
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
