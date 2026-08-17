using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PTKD.Application.CarePackages.DTOs;
using PTKD.Infrastructure.Persistence;
using PTKD.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using PTKD.Application.Common.Models;

namespace PTKD.ApiTests;

public class CarePackageRequestApiTests : IClassFixture<SafeTestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly SafeTestWebApplicationFactory _factory;
    private long _userId;
    private long _companyId;

    public CarePackageRequestApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var companyCode = "CAREPKG_" + Guid.NewGuid().ToString("N")[..8];
        var company = new PTKD.Domain.Entities.Company(companyCode, null, "Care Pkg Test Co", null);
        db.Set<PTKD.Domain.Entities.Company>().Add(company);
        await db.SaveChangesAsync();
        _companyId = company.Id;

        var (userId, token) = await SeedUserAndGetTokenAsync("carepkg_admin");
        _userId = userId;

        // Endpoint gói chăm sóc là Company-scope: filter đòi user PHẢI thuộc công ty (X-Company-Id),
        // ngoài việc có quyền. Không gán thì trả 403 "không thuộc công ty".
        await AssignUserToCompanyAsync(userId, _companyId);

        await GrantPermissionAsync(userId, "CARE_PACKAGE_CREATE", _companyId);
        await GrantPermissionAsync(userId, "CARE_PACKAGE_VIEW", _companyId);
        await GrantPermissionAsync(userId, "CARE_PACKAGE_APPROVE", _companyId);
        await GrantPermissionAsync(userId, "CARE_PACKAGE_REJECT", _companyId);
        await GrantPermissionAsync(userId, "CARE_PACKAGE_CREATE_PAYMENT", _companyId);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Add("X-Company-Id", _companyId.ToString());
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
            return; // Seeded properly in some test runs? Let's ensure it's there.

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
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            RowVersion = PTKD.Domain.ValueObjects.RowVersion.FromByteArray(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 })
        };
        db.Set<PTKD.Domain.Security.Authorization.UserIndividualPermission>().Add(up);
        await db.SaveChangesAsync();
    }

    private async Task AssignUserToCompanyAsync(long userId, long companyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbFactory.CreateDbContext();

        var exists = db.Set<PTKD.Domain.Entities.UserCompanyAssignment>()
            .Any(a => a.UserId == userId && a.CompanyId == companyId);
        if (exists) return;

        db.Set<PTKD.Domain.Entities.UserCompanyAssignment>()
            .Add(new PTKD.Domain.Entities.UserCompanyAssignment(userId, companyId, true, DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Create_MissingServiceTypeId_ReturnsBadRequest_Or_500IfUncaught()
    {
        long customerId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = new Profile(
                fullName: "Test Customer 123", cccd: null, dob: null, dobPartial: null,
                dobPrecision: null, gender: null, permanentAddress: null,
                cccdIssueDate: null, cccdIssuePlace: null, taxCode: null,
                phone: null, contactAddress: null, deathDateSolar: null,
                deathDateLunar: null, deathPlace: null, hometown: null);
            db.Set<Profile>().Add(profile);
            await db.SaveChangesAsync();
            var customer = new Customer("CUST123", profile.Id);
            customer.SetCreatedBy(_userId);
            db.Set<Customer>().Add(customer);
            await db.SaveChangesAsync();
            customerId = customer.Id;
        }

        var request = new CreateCarePackageRequest
        {
            CustomerId = customerId,
            ServiceTypeId = 0, // Missing service type id should fail
            SaleDate = DateTime.UtcNow,
            DiscountAmount = 0,
            Item = new CreateCarePackageRequestItem
            {
                GraveId = 1,
                ServicePeriodStartDate = DateTime.UtcNow
            }
        };

        var response = await _client.PostAsJsonAsync("api/v2/care-packages", request);
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        long customerId;
        long serviceTypeId;
        long graveId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var profile = new Profile(
                fullName: "Test Customer 456", cccd: null, dob: null, dobPartial: null,
                dobPrecision: null, gender: null, permanentAddress: null,
                cccdIssueDate: null, cccdIssuePlace: null, taxCode: null,
                phone: null, contactAddress: null, deathDateSolar: null,
                deathDateLunar: null, deathPlace: null, hometown: null);
            db.Set<Profile>().Add(profile);
            await db.SaveChangesAsync();
            var customer = new Customer("CUST456", profile.Id);
            customer.SetCreatedBy(_userId);
            db.Set<Customer>().Add(customer);
            await db.SaveChangesAsync();
            customerId = customer.Id;

            // Gói chăm sóc trong danh mục (giá 1000, tính theo cốt = mặc định PER_COT).
            var serviceType = new ServiceType("TEST_CARE", "Test Care", null, 1000m, 12, true, _userId);
            db.Set<ServiceType>().Add(serviceType);
            await db.SaveChangesAsync();
            serviceTypeId = serviceType.Id;

            // Phần mộ có 2 cốt, thuộc một nghĩa trang của công ty test.
            var cemetery = new Cemetery("CEM_" + Guid.NewGuid().ToString("N")[..6], _companyId, "Test Cemetery", null);
            db.Set<Cemetery>().Add(cemetery);
            await db.SaveChangesAsync();

            var grave = new Grave(
                cemeteryId: cemetery.Id, graveCode: "GRAVE-" + Guid.NewGuid().ToString("N")[..6],
                zone: "A", plotNumber: "01", graveType: Grave.TypeDouble, status: Grave.StatusEmpty,
                rowLabel: null, colLabel: null, areaM2: null, cotCount: 2, ownerCustomerId: customerId,
                emergencyContactName: null, emergencyContactPhone: null, emergencyContactRelationship: null,
                notes: null);
            db.Set<Grave>().Add(grave);
            await db.SaveChangesAsync();
            graveId = grave.Id;
        }

        var request = new CreateCarePackageRequest
        {
            CustomerId = customerId,
            ServiceTypeId = serviceTypeId,
            SaleDate = DateTime.UtcNow,
            DiscountAmount = 0,
            Item = new CreateCarePackageRequestItem
            {
                GraveId = graveId,
                ServicePeriodStartDate = DateTime.UtcNow
            }
        };

        var response = await _client.PostAsJsonAsync("api/v2/care-packages", request);
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<CarePackageRequestDto>();
        Assert.NotNull(dto);
        Assert.Equal(customerId, dto!.CustomerId);
        Assert.NotNull(dto.ServiceId); // dịch vụ của khách được tự tạo/dùng lại
        Assert.Equal(2000m, dto.SubtotalAmount); // 1000 × 2 cốt (PER_COT)
        Assert.Equal(2000m, dto.TotalAmount);
        Assert.Single(dto.Items);
        Assert.Equal(2, dto.Items[0].CotCountSnapshot); // lấy tự động từ phần mộ
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync($"api/v2/care-packages");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<CarePackageRequestDto>>();
        Assert.NotNull(result);
    }
}
