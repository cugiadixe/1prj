using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Services;
using PTKD.Domain.Entities;
using PTKD.Infrastructure.Persistence;
using PTKD.Infrastructure.Security.Cryptography;
using Xunit;

namespace PTKD.IntegrationTests;

[Collection("Sequential")]
public class AuthenticationTokenIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private ITokenSessionLifecycleService _service = null!;
    private IRefreshTokenMaterialService _materialService = null!;
    private FakeTimeProvider _timeProvider = null!;

    public AuthenticationTokenIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _fixture.ResetToV0003();

        _materialService = new RefreshTokenMaterialService();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 7, 16, 0, 0, 0, TimeSpan.Zero));
        var jwtService = new JwtAccessTokenService(
            new JwtSigningKeyProvider(
                new ConfigurationBuilder().Build(),
                NullLogger<JwtSigningKeyProvider>.Instance),
            _timeProvider);
        
        var factoryMock = new Mock<ITokenSessionDbContextFactory>();
        factoryMock.Setup(f => f.CreateDbContext()).Returns(() => CreateContext());

        _service = new TokenSessionLifecycleService(factoryMock.Object, jwtService, _materialService, _timeProvider);
        
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateSession_InsertsRow_WithHashOnly_AndNoRawTokenPersisted()
    {
        // 1. Guard check
        using var context = CreateContext();
        var dbName = context.Database.GetDbConnection().Database;
        dbName.Should().Be("PTKD_TEST_PHASE1A2", "Database guard failed.");

        var user = new User("TEST_001", "Test User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, "testuser1", "hash", _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        var result = await _service.CreateSessionAsync(account.Id, "testuser1", "127.0.0.1", "TestAgent");
        result.IsSuccess.Should().BeTrue();
        result.RefreshTokenMaterial.Should().NotBeNullOrWhiteSpace();

        var dbTokens = await context.RefreshTokens.Where(x => x.AccountId == account.Id).ToListAsync();
        dbTokens.Should().HaveCount(1);
        
        var dbToken = dbTokens.First();
        dbToken.TokenHash.Should().Be(_materialService.ComputeHash(result.RefreshTokenMaterial!));
        dbToken.TokenHash.Should().NotContain(result.RefreshTokenMaterial!); // Raw token is not stored
        
        // Expiry should be 7 days
        dbToken.ExpiresAt.Should().Be(_timeProvider.GetUtcNow().UtcDateTime.AddDays(7));
    }

    [Fact]
    public async Task RefreshSession_RotatesOldToken_ToUsedAndReplaced()
    {
        using var context = CreateContext();
        var user = new User("TEST_002", "Test User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, "testuser2", "hash", _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        var createResult = await _service.CreateSessionAsync(account.Id, "testuser2", "127.0.0.1", "TestAgent");
        var oldMaterial = createResult.RefreshTokenMaterial!;
        
        _timeProvider.Advance(TimeSpan.FromMinutes(10));
        
        var refreshResult = await _service.RefreshSessionAsync(oldMaterial, "127.0.0.1", "TestAgent");
        refreshResult.IsSuccess.Should().BeTrue();

        var dbTokens = await context.RefreshTokens.Where(x => x.AccountId == account.Id).OrderBy(x => x.Id).ToListAsync();
        dbTokens.Should().HaveCount(2);

        var oldToken = dbTokens[0];
        var newToken = dbTokens[1];

        oldToken.IsUsed.Should().BeTrue();
        oldToken.ReplacedByTokenId.Should().Be(newToken.Id);
        
        newToken.IsUsed.Should().BeFalse();
        newToken.TokenHash.Should().Be(_materialService.ComputeHash(refreshResult.RefreshTokenMaterial!));
    }

    [Fact]
    public async Task RefreshSession_Reuse_RevokesFamily()
    {
        using var context = CreateContext();
        var user = new User("TEST_003", "Test User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, "testuser3", "hash", _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        var createResult = await _service.CreateSessionAsync(account.Id, "testuser3", "127.0.0.1", "TestAgent");
        var oldMaterial = createResult.RefreshTokenMaterial!;
        
        await _service.RefreshSessionAsync(oldMaterial, "127.0.0.1", "TestAgent");
        
        // Try reuse
        var reuseResult = await _service.RefreshSessionAsync(oldMaterial, "127.0.0.1", "HackerAgent");
        reuseResult.IsSuccess.Should().BeFalse();
        reuseResult.InternalReason.Should().Be("TOKEN_REUSED");

        var dbTokens = await context.RefreshTokens.Where(x => x.AccountId == account.Id).ToListAsync();
        dbTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
        dbTokens.Should().AllSatisfy(t => t.RevokeReason.Should().Be("REUSE_DETECTED"));
    }

    [Fact]
    public async Task Logout_RevokesCurrentFamily()
    {
        using var context = CreateContext();
        var user = new User("TEST_004", "Test User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, "testuser4", "hash", _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        var createResult = await _service.CreateSessionAsync(account.Id, "testuser4", "127.0.0.1", "TestAgent");
        var refreshResult = await _service.RefreshSessionAsync(createResult.RefreshTokenMaterial!, "127.0.0.1", "TestAgent");
        
        var logoutResult = await _service.LogoutAsync(refreshResult.RefreshTokenMaterial!);
        logoutResult.IsSuccess.Should().BeTrue();

        var dbTokens = await context.RefreshTokens.Where(x => x.AccountId == account.Id).ToListAsync();
        dbTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
        dbTokens.Should().AllSatisfy(t => t.RevokeReason.Should().Be("LOGOUT"));
    }

    [Fact]
    public async Task ConcurrentRefresh_SameToken_AllowsOnlyOneSuccess()
    {
        using var context = CreateContext();
        var user = new User("TEST_005", "Test User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, "testuser5", "hash", _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        var createResult = await _service.CreateSessionAsync(account.Id, "testuser5", "127.0.0.1", "TestAgent");
        var oldMaterial = createResult.RefreshTokenMaterial!;

        var task1 = Task.Run(() => _service.RefreshSessionAsync(oldMaterial, "127.0.0.1", "TestAgent"));
        var task2 = Task.Run(() => _service.RefreshSessionAsync(oldMaterial, "127.0.0.1", "TestAgent"));
        var task3 = Task.Run(() => _service.RefreshSessionAsync(oldMaterial, "127.0.0.1", "TestAgent"));

        var results = await Task.WhenAll(task1, task2, task3);

        var successCount = results.Count(r => r.IsSuccess);
        var reuseCount = results.Count(r => r.InternalReason == "TOKEN_REUSED");

        successCount.Should().Be(1);
        reuseCount.Should().Be(2);

        var dbTokens = await context.RefreshTokens.Where(x => x.AccountId == account.Id).ToListAsync();
        dbTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public async Task Refresh_AfterAccountDisable_Denied()
    {
        using var context = CreateContext();
        var user = new User("TEST_006", "Test User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, "testuser6", "hash", _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        var createResult = await _service.CreateSessionAsync(account.Id, "testuser6", "127.0.0.1", "TestAgent");

        account.Disable(_timeProvider.GetUtcNow().UtcDateTime, user.Id);
        context.UserAuthAccounts.Update(account);
        await context.SaveChangesAsync();

        var refreshResult = await _service.RefreshSessionAsync(createResult.RefreshTokenMaterial!, "127.0.0.1", "TestAgent");
        refreshResult.IsSuccess.Should().BeFalse();
        refreshResult.InternalReason.Should().Be("ACCOUNT_DISABLED");
    }

    [Fact]
    public async Task Refresh_AfterSessionsInvalidatedAtCutoff_Denied()
    {
        using var context = CreateContext();
        var user = new User("TEST_007", "Test User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, "testuser7", "hash", _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        var createResult = await _service.CreateSessionAsync(account.Id, "testuser7", "127.0.0.1", "TestAgent");

        _timeProvider.Advance(TimeSpan.FromHours(1));
        
        // Simulate password change / admin reset
        account.InvalidateSessions(Guid.NewGuid(), _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Update(account);
        await context.SaveChangesAsync();

        _timeProvider.Advance(TimeSpan.FromHours(1));

        var refreshResult = await _service.RefreshSessionAsync(createResult.RefreshTokenMaterial!, "127.0.0.1", "TestAgent");
        refreshResult.IsSuccess.Should().BeFalse();
        refreshResult.InternalReason.Should().Be("SESSIONS_INVALIDATED_CUTOFF");
    }

    [Fact]
    public async Task Refresh_WithTokenIssuedExactlyAtCutoff_Denied()
    {
        using var context = CreateContext();
        var user = new User("TEST_007B", "Test User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, "testuser7b", "hash", _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        var createResult = await _service.CreateSessionAsync(account.Id, "testuser7b", "127.0.0.1", "TestAgent");

        // Invalidate EXACTLY AT the time the token was issued
        account.InvalidateSessions(Guid.NewGuid(), _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Update(account);
        await context.SaveChangesAsync();

        _timeProvider.Advance(TimeSpan.FromHours(1));

        var refreshResult = await _service.RefreshSessionAsync(createResult.RefreshTokenMaterial!, "127.0.0.1", "TestAgent");
        refreshResult.IsSuccess.Should().BeFalse();
        refreshResult.InternalReason.Should().Be("SESSIONS_INVALIDATED_CUTOFF");
    }

    [Fact]
    public async Task Refresh_WithTokenIssuedAfterCutoff_Allowed()
    {
        using var context = CreateContext();
        var user = new User("TEST_007C", "Test User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, "testuser7c", "hash", _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        // Admin invalidates sessions NOW
        account.InvalidateSessions(Guid.NewGuid(), _timeProvider.GetUtcNow().UtcDateTime);
        context.UserAuthAccounts.Update(account);
        await context.SaveChangesAsync();

        _timeProvider.Advance(TimeSpan.FromHours(1));
        
        // Token issued AFTER the cutoff
        var createResult = await _service.CreateSessionAsync(account.Id, "testuser7c", "127.0.0.1", "TestAgent");
        
        _timeProvider.Advance(TimeSpan.FromHours(1));

        var refreshResult = await _service.RefreshSessionAsync(createResult.RefreshTokenMaterial!, "127.0.0.1", "TestAgent");
        refreshResult.IsSuccess.Should().BeTrue();
    }
}
