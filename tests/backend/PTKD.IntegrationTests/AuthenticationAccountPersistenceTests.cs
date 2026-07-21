using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Services;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;
using PTKD.Infrastructure.Persistence;
using PTKD.Infrastructure.Persistence.Retries;
using PTKD.Infrastructure.Security.Authentication;
using PTKD.Infrastructure.Security.Audit;

namespace PTKD.IntegrationTests.Security.Authentication;

[Collection("Sequential")]
public sealed class AuthenticationAccountPersistenceTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly AuthenticationTestHarness _harness;

    public AuthenticationAccountPersistenceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetToV0003();
        _harness = new AuthenticationTestHarness(fixture);
    }

    [Fact]
    public async Task EfMapping_RoundTripsInternalAndExternalAccounts_AndVerifiesDatabaseName()
    {
        var local = await _harness.CreateInternalAccountAsync("LOCAL-USER", "synthetic-local-passphrase");
        var external = await _harness.CreateExternalAccountAsync("OIDC", "Case-Sensitive-Subject");

        await using var context = _harness.CreateContext();
        var accounts = await context.UserAuthAccounts
            .AsNoTracking()
            .OrderBy(account => account.Id)
            .ToListAsync();

        Assert.Equal(TestDatabaseSafety.ApprovedDatabaseName, _fixture.LastVerifiedDatabaseName);
        Assert.Equal(2, accounts.Count);
        Assert.Equal(local.AccountId, accounts[0].Id);
        Assert.Equal("INTERNAL", accounts[0].ProviderType);
        Assert.NotNull(accounts[0].PasswordHash);
        Assert.Null(accounts[1].PasswordHash);
        Assert.Equal("OIDC", accounts[1].ProviderType);
        Assert.Equal("Case-Sensitive-Subject", accounts[1].ProviderSubject);
        Assert.Equal(external.AccountId, accounts[1].Id);
    }

    [Fact]
    public async Task AcceptedProviderIdentityUniqueness_IsEnforced()
    {
        await _harness.CreateInternalAccountAsync("DUPLICATE-SUBJECT", "synthetic-first-passphrase");

        await using var context = _harness.CreateContext();
        var user = new User("EMP-DUP-2", "Duplicate User", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var provisional = UserAuthAccount.CreateInternal(
            user.Id,
            "DUPLICATE-SUBJECT",
            "initialization-only",
            _harness.Clock.UtcNow);
        var hash = _harness.PasswordHashService.HashPassword(provisional, "synthetic-second-passphrase");
        context.UserAuthAccounts.Add(UserAuthAccount.CreateInternal(
            user.Id,
            "DUPLICATE-SUBJECT",
            hash,
            _harness.Clock.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task RowVersion_ChangesAfterAuthenticationAccountMutation()
    {
        var seed = await _harness.CreateInternalAccountAsync("ROWVERSION-USER", "synthetic-passphrase");
        var originalVersion = seed.RowVersion;

        await using (var context = _harness.CreateContext())
        {
            var account = await context.UserAuthAccounts.SingleAsync(value => value.Id == seed.AccountId);
            account.RecordFailedAttempt(_harness.Clock.UtcNow, 5, TimeSpan.FromMinutes(15));
            await context.SaveChangesAsync();
        }

        var updated = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.False(originalVersion.SequenceEqual(updated.RowVersion));
        Assert.Equal(1, updated.FailedAttemptCount);
    }

    [Fact]
    public async Task PasswordHistory_IsAppendOnlyForUpdateAndDelete()
    {
        var seed = await _harness.CreateInternalAccountAsync("HISTORY-APPEND", "synthetic-passphrase");
        await _harness.AddHistoryAsync(seed.AccountId, "synthetic-history-passphrase", _harness.Clock.UtcNow);

        await using (var updateContext = _harness.CreateContext())
        {
            var history = await updateContext.PasswordHistories.SingleAsync();
            updateContext.Entry(history).Property(value => value.PasswordHash).CurrentValue = "modified-hash";
            await Assert.ThrowsAsync<DbUpdateException>(() => updateContext.SaveChangesAsync());
        }

        await using (var deleteContext = _harness.CreateContext())
        {
            var history = await deleteContext.PasswordHistories.SingleAsync();
            deleteContext.PasswordHistories.Remove(history);
            await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
        }

        Assert.Equal(1, await _harness.CountHistoryAsync(seed.AccountId));
    }

    [Fact]
    public async Task UserAuthAccountRelationship_HasNoCascadeDelete()
    {
        var seed = await _harness.CreateInternalAccountAsync("NO-CASCADE", "synthetic-passphrase");

        await using var context = _harness.CreateContext();
        var user = await context.Users.SingleAsync(value => value.Id == seed.UserId);
        context.Users.Remove(user);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(await _harness.LoadAccountAsync(seed.AccountId));
    }

    [Fact]
    public async Task RecentHistory_IsOrderedByCreatedAtThenIdDescending_AndLimitedToFive()
    {
        var seed = await _harness.CreateInternalAccountAsync("ORDERED-HISTORY", "synthetic-current-passphrase");
        for (var index = 1; index <= 6; index++)
        {
            await _harness.AddHistoryAsync(
                seed.AccountId,
                $"synthetic-history-{index}",
                _harness.Clock.UtcNow);
        }

        await using var context = _harness.CreateContext();
        var histories = await context.GetRecentPasswordHistoryAsync(seed.AccountId, 5);

        Assert.Equal(5, histories.Count);
        Assert.Equal(histories.OrderByDescending(value => value.Id).Select(value => value.Id), histories.Select(value => value.Id));
        Assert.Equal(6, histories[0].Id);
        Assert.Equal(2, histories[^1].Id);
    }

    [Fact]
    public void EfModel_UsesAcceptedLengthsTypesRowVersionAndIndexNames()
    {
        using var context = _harness.CreateContext();
        var account = context.Model.FindEntityType(typeof(UserAuthAccount));
        var history = context.Model.FindEntityType(typeof(PasswordHistory));

        Assert.NotNull(account);
        Assert.NotNull(history);
        Assert.Equal(30, account!.FindProperty(nameof(UserAuthAccount.ProviderType))!.GetMaxLength());
        Assert.Equal(200, account.FindProperty(nameof(UserAuthAccount.ProviderSubject))!.GetMaxLength());
        Assert.Equal(500, account.FindProperty(nameof(UserAuthAccount.PasswordHash))!.GetMaxLength());
        Assert.True(account.FindProperty(nameof(UserAuthAccount.RowVersion))!.IsConcurrencyToken);
        Assert.Equal("rowversion", account.FindProperty(nameof(UserAuthAccount.RowVersion))!.GetColumnType());
        Assert.Contains(account.GetIndexes(), index => index.GetDatabaseName() == "UQ_UserAuthAccounts_ProviderSubject" && index.IsUnique);
        Assert.Equal(500, history!.FindProperty(nameof(PasswordHistory.PasswordHash))!.GetMaxLength());
        Assert.Contains(history.GetIndexes(), index => index.GetDatabaseName() == "IX_PasswordHistory_Account_CreatedAt");
    }
}

internal sealed class AuthenticationTestHarness
{
    private readonly TestDatabaseFixture _fixture;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _employeeSequence;

    public AuthenticationTestHarness(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        using (_fixture.OpenVerifiedConnection())
        {
        }

        var connectionString = TestDatabaseSafety.ValidateConnectionString(fixture.ConnectionString);
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.ExecutionStrategy(dependencies =>
                    new DeadlockRetryPolicy(dependencies, 2, TimeSpan.FromMilliseconds(100))))
            .Options;

        Clock = new MutableUtcClock(new DateTime(2026, 7, 16, 3, 0, 0, DateTimeKind.Utc));
        PasswordHashService = new AspNetCorePasswordHashService();
        Policy = new AuthenticationAccountPolicy();
        Factory = new GuardedAuthenticationDbContextFactory(_fixture, _options);
        TransactionalAuditWriter = new SqlTransactionalAuditWriter();
        Service = new AuthenticationAccountService(
            Factory,
            PasswordHashService,
            new InternalProviderSubjectNormalizer(),
            new SecurityStampSessionInvalidationService(),
            Clock,
            Policy,
            new Moq.Mock<PTKD.Application.Security.Audit.IAuditWriter>().Object,
            TransactionalAuditWriter);
    }

    public MutableUtcClock Clock { get; }
    public AspNetCorePasswordHashService PasswordHashService { get; }
    public AuthenticationAccountPolicy Policy { get; }
    public IAuthenticationDbContextFactory Factory { get; }
    public SqlTransactionalAuditWriter TransactionalAuditWriter { get; private set; } = null!;
    public AuthenticationAccountService Service { get; }

    public AppDbContext CreateContext()
    {
        using (_fixture.OpenVerifiedConnection())
        {
        }

        return new AppDbContext(_options);
    }

    public async Task<AuthenticationAccountSeed> CreateInternalAccountAsync(
        string subject,
        string password,
        string userAccountStatus = "ACTIVE",
        string employmentStatus = "ACTIVE",
        bool mustChangePassword = false,
        DateTime? temporaryPasswordExpiresAt = null)
    {
        await using var context = CreateContext();
        var sequence = Interlocked.Increment(ref _employeeSequence);
        var user = new User(
            $"AUTH-{subject}-{sequence}",
            $"Authentication User {sequence}",
            null,
            employmentStatus,
            userAccountStatus);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var provisional = UserAuthAccount.CreateInternal(
            user.Id,
            subject,
            "initialization-only",
            Clock.UtcNow);
        var hash = PasswordHashService.HashPassword(provisional, password);
        var account = UserAuthAccount.CreateInternal(user.Id, subject, hash, Clock.UtcNow);
        if (mustChangePassword)
        {
            account.ReplacePassword(
                hash,
                true,
                temporaryPasswordExpiresAt ?? Clock.UtcNow.AddHours(24),
                Clock.UtcNow,
                user.Id);
        }

        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        return new AuthenticationAccountSeed(
            user.Id,
            account.Id,
            account.PasswordHash!,
            account.SecurityStamp,
            account.RowVersion.ToArray());
    }

    public async Task<AuthenticationAccountSeed> CreateExternalAccountAsync(string providerType, string subject)
    {
        await using var context = CreateContext();
        var sequence = Interlocked.Increment(ref _employeeSequence);
        var user = new User(
            $"AUTH-EXT-{sequence}",
            $"External Authentication User {sequence}",
            null,
            "ACTIVE",
            "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateExternal(user.Id, providerType, subject, Clock.UtcNow);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        return new AuthenticationAccountSeed(
            user.Id,
            account.Id,
            string.Empty,
            account.SecurityStamp,
            account.RowVersion.ToArray());
    }

    public async Task<UserAuthAccount> LoadAccountAsync(long accountId)
    {
        await using var context = CreateContext();
        return await context.UserAuthAccounts
            .AsNoTracking()
            .SingleAsync(account => account.Id == accountId);
    }

    public async Task AddHistoryAsync(long accountId, string password, DateTime createdAt)
    {
        await using var context = CreateContext();
        var account = await context.UserAuthAccounts.SingleAsync(value => value.Id == accountId);
        var hash = PasswordHashService.HashPassword(account, password);
        context.PasswordHistories.Add(new PasswordHistory(accountId, hash, createdAt));
        await context.SaveChangesAsync();
    }

    public async Task<int> CountHistoryAsync(long accountId)
    {
        await using var context = CreateContext();
        return await context.PasswordHistories.CountAsync(history => history.AccountId == accountId);
    }

    private sealed class GuardedAuthenticationDbContextFactory : IAuthenticationDbContextFactory
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly DbContextOptions<AppDbContext> _options;

        public GuardedAuthenticationDbContextFactory(
            TestDatabaseFixture fixture,
            DbContextOptions<AppDbContext> options)
        {
            _fixture = fixture;
            _options = options;
        }

        public IAuthenticationDbContext CreateDbContext()
        {
            using (_fixture.OpenVerifiedConnection())
            {
            }

            return new AppDbContext(_options);
        }
    }
}

internal sealed class MutableUtcClock : IUtcClock
{
    public MutableUtcClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }
}

internal sealed record AuthenticationAccountSeed(
    long UserId,
    long AccountId,
    string PasswordHash,
    Guid SecurityStamp,
    byte[] RowVersion);
