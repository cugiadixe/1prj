using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.AccountManagement;
using PTKD.Application.Security.AccountManagement.DTOs;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;
using PTKD.Infrastructure.Persistence;
using PTKD.Infrastructure.Persistence.Retries;
using PTKD.Infrastructure.Security.AccountManagement;
using PTKD.Infrastructure.Security.Audit;
using PTKD.Application.Security.Authentication.Services;
using PTKD.Infrastructure.Security.Authentication;
using PTKD.Infrastructure.Time;
using PTKD.IntegrationTests.Security.Authentication;

namespace PTKD.IntegrationTests.Security.AccountManagement;

[Collection("Sequential")]
public sealed class AccountManagementIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private readonly AccountManagementTestHarness _harness;

    public AccountManagementIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetToV0003();
        _harness = new AccountManagementTestHarness(fixture);
    }

    // UAM-I-01
    [Fact]
    public async Task GetAccountDetail_ReturnsCorrectProjection()
    {
        var seed = await _harness.CreateInternalAccountAsync("GET-DETAIL-01", "GetDetail@Pass123!");

        var dto = await _harness.Service.GetAccountDetailAsync(seed.AccountId);

        Assert.NotNull(dto);
        Assert.Equal(seed.AccountId, dto.Id);
        Assert.Equal(seed.UserId, dto.UserId);
        Assert.Equal("INTERNAL", dto.ProviderType);
        Assert.Equal("GET-DETAIL-01", dto.Username);
        Assert.Equal("ACTIVE", dto.Status);
        Assert.False(dto.MustChangePassword);
    }

    // UAM-I-02: AccountDetailDto does not expose sensitive fields (PasswordHash, SecurityStamp, RowVersion)
    [Fact]
    public void GetAccountDetail_DtoType_DoesNotExposePasswordHashOrSecurityStamp()
    {
        var dtoType = typeof(AccountDetailDto);
        var props = dtoType.GetProperties();
        var names = props.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("PasswordHash", names);
        Assert.DoesNotContain("SecurityStamp", names);
        Assert.DoesNotContain("RowVersion", names);
        Assert.DoesNotContain("SessionsInvalidatedAt", names);
    }

    // UAM-I-03
    [Fact]
    public async Task Activate_PersistsStatusChange_FromDisabled()
    {
        var seed = await _harness.CreateDisabledAccountAsync("ACTIVATE-DISABLED-03");

        var result = await _harness.Service.ActivateAccountAsync(seed.AccountId, actingUserId: seed.UserId);

        Assert.True(result.Succeeded);
        var loaded = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.Equal("ACTIVE", loaded.AuthAccountStatus);
        Assert.Equal(0, loaded.FailedAttemptCount);
        Assert.Null(loaded.LockoutEnd);
    }

    // UAM-I-04
    [Fact]
    public async Task Activate_WritesAuditEventAtomically()
    {
        var seed = await _harness.CreateDisabledAccountAsync("ACTIVATE-AUDIT-04");

        await _harness.Service.ActivateAccountAsync(seed.AccountId, actingUserId: seed.UserId);

        var auditRow = _harness.FindAuditEvent("ACCOUNT_ACTIVATED", seed.AccountId);
        Assert.NotNull(auditRow);
        Assert.Equal("SUCCESS", auditRow.Outcome);
        Assert.Equal("AUTH_ACCOUNT", auditRow.EntityType);
        Assert.Equal(seed.UserId, auditRow.ActorUserId);
    }

    // UAM-I-05
    [Fact]
    public async Task Disable_PersistsStatusChange()
    {
        var seed = await _harness.CreateInternalAccountAsync("DISABLE-05", "Disable@Pass123!");
        var actor = await _harness.CreateInternalAccountAsync("DISABLE-05-ACTOR", "Actor@Pass123!");

        var result = await _harness.Service.DisableAccountAsync(seed.AccountId, "Test disable", actingUserId: actor.UserId);

        Assert.True(result.Succeeded);
        var loaded = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.Equal("DISABLED", loaded.AuthAccountStatus);
    }

    // UAM-I-06
    [Fact]
    public async Task Disable_InvalidatesSessions()
    {
        var seed = await _harness.CreateInternalAccountAsync("DISABLE-SESSIONS-06", "Disable@Pass123!");
        var actor = await _harness.CreateInternalAccountAsync("DISABLE-SESSIONS-06-ACTOR", "Actor@Pass123!");
        var before = await _harness.LoadAccountAsync(seed.AccountId);

        await _harness.Service.DisableAccountAsync(seed.AccountId, "Invalidate", actingUserId: actor.UserId);

        var after = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.NotEqual(before.SecurityStamp, after.SecurityStamp);
    }

    // UAM-I-07
    [Fact]
    public async Task Disable_WritesAuditWithReason()
    {
        var seed = await _harness.CreateInternalAccountAsync("DISABLE-AUDIT-07", "Disable@Pass123!");
        var actor = await _harness.CreateInternalAccountAsync("DISABLE-AUDIT-07-ACTOR", "Actor@Pass123!");

        await _harness.Service.DisableAccountAsync(seed.AccountId, "Policy violation UAM-I-07", actingUserId: actor.UserId);

        var auditRow = _harness.FindAuditEvent("ACCOUNT_DISABLED", seed.AccountId);
        Assert.NotNull(auditRow);
        Assert.Equal("Policy violation UAM-I-07", auditRow.Reason);
        Assert.Equal("SUCCESS", auditRow.Outcome);
    }

    // UAM-I-08
    [Fact]
    public async Task Lock_PersistsStatusChange()
    {
        var seed = await _harness.CreateInternalAccountAsync("LOCK-08", "Lock@Pass123!");
        var actor = await _harness.CreateInternalAccountAsync("LOCK-08-ACTOR", "Actor@Pass123!");

        var result = await _harness.Service.LockAccountAsync(seed.AccountId, "Suspicious activity", actingUserId: actor.UserId);

        Assert.True(result.Succeeded);
        var loaded = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.Equal("LOCKED", loaded.AuthAccountStatus);
    }

    // UAM-I-09
    [Fact]
    public async Task Lock_SetsManualLockWithNoExpiry()
    {
        var seed = await _harness.CreateInternalAccountAsync("LOCK-MANUAL-09", "Lock@Pass123!");
        var actor = await _harness.CreateInternalAccountAsync("LOCK-MANUAL-09-ACTOR", "Actor@Pass123!");

        await _harness.Service.LockAccountAsync(seed.AccountId, "Manual lock", actingUserId: actor.UserId);

        var loaded = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.True(loaded.IsManualLock);
        Assert.Null(loaded.LockoutEnd);
    }

    // UAM-I-10
    [Fact]
    public async Task Lock_DisabledAccount_ReturnsConflict()
    {
        var seed = await _harness.CreateDisabledAccountAsync("LOCK-DISABLED-10");
        var actor = await _harness.CreateInternalAccountAsync("LOCK-DISABLED-10-ACTOR", "Actor@Pass123!");

        var result = await _harness.Service.LockAccountAsync(seed.AccountId, "Lock disabled", actingUserId: actor.UserId);

        Assert.False(result.Succeeded);
        Assert.Equal("AUTH_ACCOUNT_STATE_CONFLICT", result.ErrorCode);
    }

    // UAM-I-11
    [Fact]
    public async Task Unlock_PersistsStatusChange()
    {
        var seed = await _harness.CreateLockedAccountAsync("UNLOCK-11");

        var result = await _harness.Service.UnlockAccountAsync(seed.AccountId, actingUserId: seed.UserId);

        Assert.True(result.Succeeded);
        var loaded = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.Equal("ACTIVE", loaded.AuthAccountStatus);
    }

    // UAM-I-12
    [Fact]
    public async Task Unlock_DisabledAccount_ReturnsConflict()
    {
        var seed = await _harness.CreateDisabledAccountAsync("UNLOCK-DISABLED-12");

        var result = await _harness.Service.UnlockAccountAsync(seed.AccountId, actingUserId: seed.UserId);

        Assert.False(result.Succeeded);
        Assert.Equal("AUTH_ACCOUNT_STATE_CONFLICT", result.ErrorCode);
    }

    // UAM-I-13
    [Fact]
    public async Task AdminResetPassword_GeneratesNonEmptyTemporaryPassword()
    {
        var seed = await _harness.CreateInternalAccountAsync("RESET-PWD-13", "Reset@Pass123!");

        var result = await _harness.Service.AdminResetPasswordAsync(seed.AccountId, "Admin reset", actingUserId: seed.UserId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.TemporaryPassword);
        Assert.True(result.TemporaryPassword.Length >= 8);
    }

    // UAM-I-14
    [Fact]
    public async Task AdminResetPassword_SetsMustChangePassword()
    {
        var seed = await _harness.CreateInternalAccountAsync("RESET-MCP-14", "Reset@Pass123!");

        await _harness.Service.AdminResetPasswordAsync(seed.AccountId, "Force change", actingUserId: seed.UserId);

        var loaded = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.True(loaded.MustChangePassword);
    }

    // UAM-I-14b
    [Fact]
    public async Task AdminResetPassword_SetsTemporaryPasswordExpiry()
    {
        var seed = await _harness.CreateInternalAccountAsync("RESET-EXPIRY-14B", "Reset@Pass123!");

        await _harness.Service.AdminResetPasswordAsync(seed.AccountId, "Expiry check", actingUserId: seed.UserId);

        var loaded = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.NotNull(loaded.TemporaryPasswordExpiresAt);
        Assert.True(loaded.TemporaryPasswordExpiresAt > _harness.Clock.UtcNow);
    }

    // UAM-I-15
    [Fact]
    public async Task AdminResetPassword_WritesPasswordHistory()
    {
        var seed = await _harness.CreateInternalAccountAsync("RESET-HIST-15", "Reset@Pass123!");

        await _harness.Service.AdminResetPasswordAsync(seed.AccountId, "History check", actingUserId: seed.UserId);

        var historyCount = await _harness.CountHistoryAsync(seed.AccountId);
        Assert.True(historyCount > 0);
    }

    // UAM-I-16
    [Fact]
    public async Task AdminResetPassword_AuditEventDoesNotContainPasswordMaterial()
    {
        var seed = await _harness.CreateInternalAccountAsync("RESET-AUDIT-16", "Reset@Pass123!");

        var result = await _harness.Service.AdminResetPasswordAsync(seed.AccountId, "Audit check", actingUserId: seed.UserId);

        Assert.NotNull(result.TemporaryPassword);
        var auditRow = _harness.FindAuditEvent("ACCOUNT_PASSWORD_RESET_BY_ADMIN", seed.AccountId);
        Assert.NotNull(auditRow);

        // The temporary password must not appear in any audit column
        var auditValues = new[]
        {
            auditRow.Reason,
            auditRow.EntityType,
            auditRow.EventCode,
            auditRow.Outcome
        };
        foreach (var val in auditValues)
        {
            if (val is not null)
                Assert.DoesNotContain(result.TemporaryPassword, val, StringComparison.Ordinal);
        }
    }

    // UAM-I-17
    [Fact]
    public async Task AdminResetPassword_InvalidatesExistingSessions()
    {
        var seed = await _harness.CreateInternalAccountAsync("RESET-SESSION-17", "Reset@Pass123!");
        var before = await _harness.LoadAccountAsync(seed.AccountId);

        await _harness.Service.AdminResetPasswordAsync(seed.AccountId, "Session invalidate", actingUserId: seed.UserId);

        var after = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.NotEqual(before.SecurityStamp, after.SecurityStamp);
    }

    // UAM-I-18
    [Fact]
    public async Task RevokeSessions_DoesNotChangeStatusOrPassword()
    {
        var seed = await _harness.CreateInternalAccountAsync("REVOKE-NOCHANGE-18", "Revoke@Pass123!");
        var before = await _harness.LoadAccountAsync(seed.AccountId);

        var result = await _harness.Service.RevokeAllSessionsAsync(seed.AccountId, "Incident response", actingUserId: seed.UserId);

        Assert.True(result.Succeeded);
        var after = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.Equal(before.AuthAccountStatus, after.AuthAccountStatus);
        Assert.Equal(before.PasswordHash, after.PasswordHash);
    }

    // UAM-I-19
    [Fact]
    public async Task RevokeSessions_BumpsSecurityStamp()
    {
        var seed = await _harness.CreateInternalAccountAsync("REVOKE-STAMP-19", "Revoke@Pass123!");
        var before = await _harness.LoadAccountAsync(seed.AccountId);

        await _harness.Service.RevokeAllSessionsAsync(seed.AccountId, "Security audit", actingUserId: seed.UserId);

        var after = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.NotEqual(before.SecurityStamp, after.SecurityStamp);
    }

    // UAM-I-20
    [Fact]
    public async Task RevokeSessions_WritesAuditWithReason()
    {
        var seed = await _harness.CreateInternalAccountAsync("REVOKE-AUDIT-20", "Revoke@Pass123!");

        await _harness.Service.RevokeAllSessionsAsync(seed.AccountId, "Revoke audit UAM-I-20", actingUserId: seed.UserId);

        var auditRow = _harness.FindAuditEvent("ACCOUNT_SESSIONS_REVOKED", seed.AccountId);
        Assert.NotNull(auditRow);
        Assert.Equal("Revoke audit UAM-I-20", auditRow.Reason);
    }

    // UAM-I-21
    [Fact]
    public async Task NotFound_ReturnsAuthAccountNotFoundErrorCode()
    {
        var result = await _harness.Service.GetAccountDetailAsync(long.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task NotFound_MutationReturnsAuthAccountNotFoundErrorCode()
    {
        var result = await _harness.Service.ActivateAccountAsync(long.MaxValue, actingUserId: 1);

        Assert.False(result.Succeeded);
        Assert.Equal("AUTH_ACCOUNT_NOT_FOUND", result.ErrorCode);
    }
}

internal sealed class AccountManagementTestHarness
{
    private readonly TestDatabaseFixture _fixture;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _sequence;

    public AccountManagementTestHarness(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        using (_fixture.OpenVerifiedConnection()) { }

        var connectionString = TestDatabaseSafety.ValidateConnectionString(fixture.ConnectionString);
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.ExecutionStrategy(deps =>
                    new DeadlockRetryPolicy(deps, 2, TimeSpan.FromMilliseconds(100))))
            .Options;

        Clock = new MutableUtcClock(new DateTime(2026, 7, 21, 9, 0, 0, DateTimeKind.Utc));
        var passwordHashService = new AspNetCorePasswordHashService();
        var factory = new GuardedAuthDbContextFactory(fixture, _options);

        Service = new AccountManagementService(
            factory,
            // Dịch vụ nay cần thêm ngữ cảnh tổ chức (để lấy công ty/phòng ban của nhân viên).
            new AppDbContextFactory(_options),
            passwordHashService,
            new SecurityStampSessionInvalidationService(),
            new SqlTransactionalAuditWriter(),
            Clock,
            new AuthenticationAccountPolicy(),
            new PTKD.Application.Security.Authorization.Services.AdminSafetyService(new AppDbContext(_options)));
    }

    public MutableUtcClock Clock { get; }
    public AccountManagementService Service { get; }

    public AppDbContext CreateContext()
    {
        using (_fixture.OpenVerifiedConnection()) { }
        return new AppDbContext(_options);
    }

    public async Task<AuthenticationAccountSeed> CreateInternalAccountAsync(string subject, string password)
    {
        await using var context = CreateContext();
        var seq = Interlocked.Increment(ref _sequence);
        var user = new User($"AM-{subject}-{seq}", $"AM User {seq}", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var provisional = UserAuthAccount.CreateInternal(user.Id, subject, "placeholder", Clock.UtcNow);
        var hasher = new AspNetCorePasswordHashService();
        var hash = hasher.HashPassword(provisional, password);
        var account = UserAuthAccount.CreateInternal(user.Id, subject, hash, Clock.UtcNow);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        return new AuthenticationAccountSeed(user.Id, account.Id, account.PasswordHash!, account.SecurityStamp, account.RowVersion.ToArray());
    }

    public async Task<AuthenticationAccountSeed> CreateDisabledAccountAsync(string subject)
    {
        await using var context = CreateContext();
        var seq = Interlocked.Increment(ref _sequence);
        var user = new User($"AM-DIS-{subject}-{seq}", $"AM Disabled {seq}", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, subject, "hash_disabled", Clock.UtcNow);
        account.Disable(Clock.UtcNow, user.Id);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        return new AuthenticationAccountSeed(user.Id, account.Id, account.PasswordHash ?? "", account.SecurityStamp, account.RowVersion.ToArray());
    }

    public async Task<AuthenticationAccountSeed> CreateLockedAccountAsync(string subject)
    {
        await using var context = CreateContext();
        var seq = Interlocked.Increment(ref _sequence);
        var user = new User($"AM-LCK-{subject}-{seq}", $"AM Locked {seq}", null, "ACTIVE", "ACTIVE");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, subject, "hash_locked", Clock.UtcNow);
        account.Lock(Clock.UtcNow, user.Id);
        context.UserAuthAccounts.Add(account);
        await context.SaveChangesAsync();

        return new AuthenticationAccountSeed(user.Id, account.Id, account.PasswordHash ?? "", account.SecurityStamp, account.RowVersion.ToArray());
    }

    public async Task<UserAuthAccount> LoadAccountAsync(long accountId)
    {
        await using var context = CreateContext();
        return await context.UserAuthAccounts.AsNoTracking().SingleAsync(a => a.Id == accountId);
    }

    public async Task<int> CountHistoryAsync(long accountId)
    {
        await using var context = CreateContext();
        return await context.PasswordHistories.CountAsync(h => h.AccountId == accountId);
    }

    public AuditRow? FindAuditEvent(string eventCode, long entityId)
    {
        using var conn = _fixture.OpenVerifiedConnection();
        using var cmd = new SqlCommand(
            """
            SELECT TOP 1 event_code, entity_type, entity_id, outcome, reason, actor_user_id
            FROM dbo.Security_Audit_Events
            WHERE event_code = @eventCode AND entity_id = @entityId
            ORDER BY id DESC;
            """, conn);
        cmd.Parameters.AddWithValue("@eventCode", eventCode);
        cmd.Parameters.AddWithValue("@entityId", entityId.ToString());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new AuditRow(
            EventCode: reader.GetString(0),
            EntityType: reader.GetString(1),
            EntityId: reader.GetString(2),
            Outcome: reader.GetString(3),
            Reason: reader.IsDBNull(4) ? null : reader.GetString(4),
            ActorUserId: reader.IsDBNull(5) ? null : reader.GetInt64(5));
    }

    private sealed class GuardedAuthDbContextFactory : IAuthenticationDbContextFactory
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly DbContextOptions<AppDbContext> _options;

        public GuardedAuthDbContextFactory(
            TestDatabaseFixture fixture,
            DbContextOptions<AppDbContext> options)
        {
            _fixture = fixture;
            _options = options;
        }

        public IAuthenticationDbContext CreateDbContext()
        {
            using (_fixture.OpenVerifiedConnection()) { }
            return new AppDbContext(_options);
        }
    }
}

internal sealed record AuditRow(
    string EventCode,
    string EntityType,
    string EntityId,
    string Outcome,
    string? Reason,
    long? ActorUserId);
