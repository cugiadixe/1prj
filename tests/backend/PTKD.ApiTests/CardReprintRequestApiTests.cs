using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PTKD.Application.Cards.DTOs;
using PTKD.Infrastructure.Persistence;
using PTKD.Domain.Entities;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace PTKD.ApiTests;

public class CardReprintRequestApiTests : IClassFixture<SafeTestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly SafeTestWebApplicationFactory _factory;
    private long _userId;
    private long _companyId;

    public CardReprintRequestApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var companyCode = "REPRINT_" + Guid.NewGuid().ToString("N")[..8];
        var company = new PTKD.Domain.Entities.Company(companyCode, null, "Reprint Test Co", null);
        db.Set<PTKD.Domain.Entities.Company>().Add(company);
        await db.SaveChangesAsync();
        _companyId = company.Id;

        var (userId, token) = await SeedUserAndGetTokenAsync("reprint_admin");
        _userId = userId;

        await GrantPermissionAsync(userId, "CARD_REPRINT_REQUEST_CREATE", _companyId);
        await GrantPermissionAsync(userId, "CARD_REPRINT_REQUEST_VIEW", _companyId);

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

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        long cardId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var card = Card.Create(_companyId, "GRAVE-123", null, _userId);
            db.Set<Card>().Add(card);
            await db.SaveChangesAsync();
            cardId = card.Id;
        }

        var request = new CreateCardReprintRequest
        {
            CompanyId = _companyId,
            CardId = cardId,
            ReasonCode = "LOST",
            Notes = "Lost card"
        };

        var response = await _client.PostAsJsonAsync("api/v2/card-reprint-requests", request);
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<CardReprintRequestDto>();
        Assert.NotNull(dto);
        Assert.Equal(cardId, dto!.CardId);
        Assert.Equal(_companyId, dto.CompanyId);
        Assert.Equal(CardReprintRequest.StatusDraft, dto.Status);
        Assert.Equal(CardReprintRequest.TypeInitialPrint, dto.RequestType);
    }

    [Fact]
    public async Task GetById_ExistingRequest_ReturnsOk()
    {
        long cardId;
        long requestId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var card = Card.Create(_companyId, "GRAVE-456", null, _userId);
            db.Set<Card>().Add(card);
            await db.SaveChangesAsync();
            
            var req = CardReprintRequest.CreateDraft(_companyId, card.Id, _userId, CardReprintRequest.TypeReprint, 1, "DAMAGED", "Notes", _userId);
            db.Set<CardReprintRequest>().Add(req);
            await db.SaveChangesAsync();
            
            cardId = card.Id;
            requestId = req.Id;
        }

        var response = await _client.GetAsync($"api/v2/card-reprint-requests/{requestId}");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<CardReprintRequestDto>();
        Assert.NotNull(dto);
        Assert.Equal(requestId, dto!.Id);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        long cardId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var card = Card.Create(_companyId, "GRAVE-999", null, _userId);
            db.Set<Card>().Add(card);
            await db.SaveChangesAsync();
            
            var req = CardReprintRequest.CreateDraft(_companyId, card.Id, _userId, CardReprintRequest.TypeReprint, 1, "DAMAGED", "Notes", _userId);
            db.Set<CardReprintRequest>().Add(req);
            await db.SaveChangesAsync();
            
            cardId = card.Id;
        }

        var response = await _client.GetAsync($"api/v2/card-reprint-requests");
        response.EnsureSuccessStatusCode();

        var dtos = await response.Content.ReadFromJsonAsync<List<CardReprintRequestDto>>();
        Assert.NotNull(dtos);
        Assert.NotEmpty(dtos!);
    }

    [Fact]
    public async Task Create_InvalidCardId_ReturnsNotFound()
    {
        var request = new CreateCardReprintRequest
        {
            CompanyId = _companyId,
            CardId = 999999, // Invalid ID
            ReasonCode = "LOST",
            Notes = "Lost card"
        };

        var response = await _client.PostAsJsonAsync("api/v2/card-reprint-requests", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingCompanyHeader_ReturnsBadRequest()
    {
        var request = new CreateCardReprintRequest
        {
            CompanyId = _companyId,
            CardId = 1,
            ReasonCode = "LOST"
        };
        
        var clientWithoutHeader = _factory.CreateClient();
        clientWithoutHeader.DefaultRequestHeaders.Authorization = _client.DefaultRequestHeaders.Authorization;
        
        var response = await clientWithoutHeader.PostAsJsonAsync("api/v2/card-reprint-requests", request);
        // Will be 400 or 403 because header is missing
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_CrossCompany_ReturnsForbidden()
    {
        long otherCompanyId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var otherCompanyCode = "OTHER_" + Guid.NewGuid().ToString("N")[..8];
            var otherCompany = new PTKD.Domain.Entities.Company(otherCompanyCode, null, "Other Company", null);
            db.Set<PTKD.Domain.Entities.Company>().Add(otherCompany);
            await db.SaveChangesAsync();
            otherCompanyId = otherCompany.Id;
        }

        var request = new CreateCardReprintRequest
        {
            CompanyId = otherCompanyId,
            CardId = 1,
            ReasonCode = "LOST"
        };
        
        var clientCrossCompany = _factory.CreateClient();
        clientCrossCompany.DefaultRequestHeaders.Authorization = _client.DefaultRequestHeaders.Authorization;
        clientCrossCompany.DefaultRequestHeaders.Add("X-Company-Id", otherCompanyId.ToString());
        
        // This should be forbidden because the user doesn't have permission for otherCompanyId
        var response = await clientCrossCompany.PostAsJsonAsync("api/v2/card-reprint-requests", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
