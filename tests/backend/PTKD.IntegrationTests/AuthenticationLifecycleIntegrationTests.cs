using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Models;
using PTKD.Application.Security.Authentication.Services;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;

namespace PTKD.IntegrationTests.Security.Authentication;

[Collection("Sequential")]
public sealed class AuthenticationLifecycleIntegrationTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly AuthenticationTestHarness _harness;

    public AuthenticationLifecycleIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetToV0003();
        _harness = new AuthenticationTestHarness(fixture);
    }

    [Fact]
    public async Task FailedAttempts_FifthLocks_ActiveLockoutDoesNotIncrement_AndResultIsGeneric()
    {
        var seed = await _harness.CreateInternalAccountAsync("LOCKOUT-USER", "synthetic-correct-passphrase");
        AuthenticationAttemptResult? lastResult = null;

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            lastResult = await AuthenticateAsync("LOCKOUT-USER", "synthetic-wrong-passphrase");
            Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, lastResult.ErrorCode);
        }

        var locked = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.Equal(5, locked.FailedAttemptCount);
        Assert.Equal(AuthenticationAccountPolicy.LockedAccountStatus, locked.AuthAccountStatus);
        Assert.Equal(_harness.Clock.UtcNow.AddMinutes(15), locked.LockoutEnd);

        var activeLockoutResult = await AuthenticateAsync("LOCKOUT-USER", "synthetic-correct-passphrase");
        var unknownResult = await AuthenticateAsync("UNKNOWN-USER", "synthetic-correct-passphrase");
        var afterActiveLockout = await _harness.LoadAccountAsync(seed.AccountId);

        Assert.Equal(lastResult, activeLockoutResult);
        Assert.Equal(lastResult, unknownResult);
        Assert.Equal(5, afterActiveLockout.FailedAttemptCount);
        Assert.Equal(locked.LockoutEnd, afterActiveLockout.LockoutEnd);
    }

    [Fact]
    public async Task ExpiredLockout_NextFailedAttemptAtomicallyBecomesAttemptOne()
    {
        var seed = await _harness.CreateInternalAccountAsync("EXPIRED-LOCKOUT", "synthetic-correct-passphrase");
        for (var attempt = 0; attempt < 5; attempt++)
            await AuthenticateAsync("EXPIRED-LOCKOUT", "synthetic-wrong-passphrase");

        _harness.Clock.UtcNow = _harness.Clock.UtcNow.AddMinutes(15);
        await AuthenticateAsync("EXPIRED-LOCKOUT", "synthetic-wrong-passphrase");

        var account = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
        Assert.Equal(1, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
    }

    [Fact]
    public async Task SuccessfulAuthentication_ResetsFailuresAndReturnsSafeIdentityState()
    {
        var seed = await _harness.CreateInternalAccountAsync("SUCCESS-RESET", "synthetic-correct-passphrase");
        await AuthenticateAsync("SUCCESS-RESET", "synthetic-wrong-passphrase");
        await AuthenticateAsync("SUCCESS-RESET", "synthetic-wrong-passphrase");

        var result = await AuthenticateAsync("SUCCESS-RESET", "synthetic-correct-passphrase");
        var account = await _harness.LoadAccountAsync(seed.AccountId);

        Assert.Equal(AuthenticationAttemptOutcome.Succeeded, result.Outcome);
        Assert.Equal(seed.AccountId, result.AccountId);
        Assert.Equal(seed.UserId, result.UserId);
        Assert.Null(result.ErrorCode);
        Assert.Equal(0, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
    }

    [Fact]
    public async Task IneligibleAndDisabledAccounts_DoNotAccumulateFailures()
    {
        var seeds = new List<(AuthenticationAccountSeed Seed, string Subject)>
        {
            (await _harness.CreateInternalAccountAsync("USER-SUSPENDED", "synthetic-passphrase", "SUSPENDED", "ACTIVE"), "USER-SUSPENDED"),
            (await _harness.CreateInternalAccountAsync("EMP-SUSPENDED", "synthetic-passphrase", "ACTIVE", "SUSPENDED"), "EMP-SUSPENDED"),
            (await _harness.CreateInternalAccountAsync("EMP-TERMINATED", "synthetic-passphrase", "ACTIVE", "TERMINATED"), "EMP-TERMINATED"),
            (await _harness.CreateInternalAccountAsync("EMP-RETIRED", "synthetic-passphrase", "ACTIVE", "RETIRED"), "EMP-RETIRED"),
            (await _harness.CreateInternalAccountAsync("EMP-RESIGNED", "synthetic-passphrase", "ACTIVE", "RESIGNED"), "EMP-RESIGNED"),
            (await _harness.CreateInternalAccountAsync("EMP-INACTIVE", "synthetic-passphrase", "ACTIVE", "INACTIVE"), "EMP-INACTIVE")
        };

        var disabledSeed = await _harness.CreateInternalAccountAsync("AUTH-DISABLED", "synthetic-passphrase");
        var disableResult = await _harness.Service.DisableAccountAsync(new DisableAuthenticationAccountCommand(
            disabledSeed.AccountId,
            disabledSeed.RowVersion,
            disabledSeed.UserId));
        Assert.True(disableResult.Succeeded);
        seeds.Add((disabledSeed, "AUTH-DISABLED"));

        foreach (var item in seeds)
        {
            var result = await AuthenticateAsync(item.Subject, "synthetic-wrong-passphrase");
            Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, result.ErrorCode);
            Assert.Equal(0, (await _harness.LoadAccountAsync(item.Seed.AccountId)).FailedAttemptCount);
        }
    }

    [Fact]
    public async Task SuccessRehashNeeded_UpdatesHashAndRowVersionWithoutHistoryStampOrTemporaryChange()
    {
        const string password = "synthetic-legacy-passphrase";
        var seed = await _harness.CreateInternalAccountAsync(
            "REHASH-USER",
            password,
            mustChangePassword: true,
            temporaryPasswordExpiresAt: _harness.Clock.UtcNow.AddHours(24));

        string legacyHash;
        Guid stamp;
        byte[] preRehashVersion;
        DateTime? expiry;
        await using (var context = _harness.CreateContext())
        {
            var account = await context.UserAuthAccounts.SingleAsync(value => value.Id == seed.AccountId);
            var legacyHasher = new PasswordHasher<UserAuthAccount>(Options.Create(new PasswordHasherOptions
            {
                CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
            }));
            legacyHash = legacyHasher.HashPassword(account, password);
            context.Entry(account).Property(value => value.PasswordHash).CurrentValue = legacyHash;
            await context.SaveChangesAsync();
            stamp = account.SecurityStamp;
            preRehashVersion = account.RowVersion.ToArray();
            expiry = account.TemporaryPasswordExpiresAt;
        }

        var result = await AuthenticateAsync("REHASH-USER", password);
        var accountAfter = await _harness.LoadAccountAsync(seed.AccountId);

        Assert.Equal(AuthenticationAttemptOutcome.PasswordChangeRequired, result.Outcome);
        Assert.NotEqual(legacyHash, accountAfter.PasswordHash);
        Assert.False(preRehashVersion.SequenceEqual(accountAfter.RowVersion));
        Assert.Equal(stamp, accountAfter.SecurityStamp);
        Assert.True(accountAfter.MustChangePassword);
        Assert.Equal(expiry, accountAfter.TemporaryPasswordExpiresAt);
        Assert.Equal(0, await _harness.CountHistoryAsync(seed.AccountId));
    }

    [Fact]
    public async Task PasswordChange_AppendsOutgoingHashClearsTemporaryStateAndRotatesStampAtomically()
    {
        var seed = await _harness.CreateInternalAccountAsync(
            "CHANGE-USER",
            "synthetic-current-passphrase",
            mustChangePassword: true,
            temporaryPasswordExpiresAt: _harness.Clock.UtcNow.AddHours(24));

        var result = await _harness.Service.ChangePasswordAsync(new ChangePasswordCommand(
            seed.AccountId,
            "synthetic-current-passphrase",
            "synthetic-replacement-passphrase",
            seed.RowVersion,
            seed.UserId));
        var account = await _harness.LoadAccountAsync(seed.AccountId);

        Assert.True(result.Succeeded);
        Assert.False(account.MustChangePassword);
        Assert.Null(account.TemporaryPasswordExpiresAt);
        Assert.NotEqual(seed.SecurityStamp, account.SecurityStamp);
        Assert.Equal(_harness.Clock.UtcNow, account.SessionsInvalidatedAt);
        Assert.Equal(0, account.FailedAttemptCount);
        Assert.Equal(
            PasswordHashVerificationResult.Succeeded,
            _harness.PasswordHashService.VerifyPassword(account, account.PasswordHash, "synthetic-replacement-passphrase"));

        await using var context = _harness.CreateContext();
        var history = await context.PasswordHistories.SingleAsync(value => value.AccountId == seed.AccountId);
        Assert.Equal(seed.PasswordHash, history.PasswordHash);
    }

    [Fact]
    public async Task PasswordHistory_RejectsCurrentAndLatestFive_ButAllowsSixthOlderPassword()
    {
        var seed = await _harness.CreateInternalAccountAsync("HISTORY-WINDOW", "synthetic-current-passphrase");
        for (var index = 1; index <= 6; index++)
        {
            await _harness.AddHistoryAsync(
                seed.AccountId,
                $"synthetic-historic-passphrase-{index}",
                _harness.Clock.UtcNow.AddMinutes(-index));
        }

        var currentReuse = await _harness.Service.ChangePasswordAsync(new ChangePasswordCommand(
            seed.AccountId,
            "synthetic-current-passphrase",
            "synthetic-current-passphrase",
            seed.RowVersion,
            seed.UserId));
        for (var index = 1; index <= 5; index++)
        {
            var recentReuse = await _harness.Service.ChangePasswordAsync(new ChangePasswordCommand(
                seed.AccountId,
                "synthetic-current-passphrase",
                $"synthetic-historic-passphrase-{index}",
                seed.RowVersion,
                seed.UserId));

            Assert.Equal(AuthenticationErrorCodes.PasswordReuse, recentReuse.ErrorCode);
        }

        var sixthOlder = await _harness.Service.ChangePasswordAsync(new ChangePasswordCommand(
            seed.AccountId,
            "synthetic-current-passphrase",
            "synthetic-historic-passphrase-6",
            seed.RowVersion,
            seed.UserId));

        Assert.Equal(AuthenticationErrorCodes.PasswordReuse, currentReuse.ErrorCode);
        Assert.True(sixthOlder.Succeeded);
        Assert.Equal(7, await _harness.CountHistoryAsync(seed.AccountId));
    }

    [Fact]
    public async Task AdministratorReset_LocalAccountCreates24HourTemporaryCredentialAndClearsLockout()
    {
        var seed = await _harness.CreateInternalAccountAsync("ADMIN-RESET", "synthetic-current-passphrase");
        for (var attempt = 0; attempt < 5; attempt++)
            await AuthenticateAsync("ADMIN-RESET", "synthetic-wrong-passphrase");

        var locked = await _harness.LoadAccountAsync(seed.AccountId);
        var result = await _harness.Service.AdministratorResetPasswordAsync(new AdministratorResetPasswordCommand(
            seed.AccountId,
            "synthetic-temporary-passphrase",
            locked.RowVersion,
            seed.UserId));
        var account = await _harness.LoadAccountAsync(seed.AccountId);

        Assert.True(result.Succeeded);
        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
        Assert.Equal(0, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
        Assert.True(account.MustChangePassword);
        Assert.Equal(_harness.Clock.UtcNow.AddHours(24), account.TemporaryPasswordExpiresAt);
        Assert.NotEqual(locked.SecurityStamp, account.SecurityStamp);
        Assert.Equal(_harness.Clock.UtcNow, account.SessionsInvalidatedAt);
        Assert.Equal(1, await _harness.CountHistoryAsync(seed.AccountId));
    }

    [Fact]
    public async Task AdministratorReset_ExternalAccountIsRejectedWithoutMutation()
    {
        var seed = await _harness.CreateExternalAccountAsync("OIDC", "external-reset-subject");

        var result = await _harness.Service.AdministratorResetPasswordAsync(new AdministratorResetPasswordCommand(
            seed.AccountId,
            "synthetic-temporary-passphrase",
            seed.RowVersion,
            seed.UserId));
        var account = await _harness.LoadAccountAsync(seed.AccountId);

        Assert.False(result.Succeeded);
        Assert.Equal(AuthenticationErrorCodes.ExternalPasswordManaged, result.ErrorCode);
        Assert.Null(account.PasswordHash);
        Assert.Equal(seed.SecurityStamp, account.SecurityStamp);
        Assert.True(seed.RowVersion.SequenceEqual(account.RowVersion));
        Assert.Equal(0, await _harness.CountHistoryAsync(seed.AccountId));
    }

    [Fact]
    public async Task ExternalAuthentication_AlwaysUsesDummyHashPathEvenIfPersistedHashIsCorrupted()
    {
        var seed = await _harness.CreateExternalAccountAsync("OIDC", "external-dummy-subject");
        var recordingHasher = new RecordingPasswordHashService(_harness.PasswordHashService);
        string corruptedHash;

        await using (var context = _harness.CreateContext())
        {
            var account = await context.UserAuthAccounts.SingleAsync(value => value.Id == seed.AccountId);
            var internalHashIdentity = UserAuthAccount.CreateInternal(
                seed.UserId,
                "HASH-IDENTITY",
                "initialization-only",
                _harness.Clock.UtcNow);
            corruptedHash = _harness.PasswordHashService.HashPassword(
                internalHashIdentity,
                "synthetic-external-passphrase");
            context.Entry(account).Property(value => value.PasswordHash).CurrentValue = corruptedHash;
            await context.SaveChangesAsync();
        }

        var service = new AuthenticationAccountService(
            _harness.Factory,
            recordingHasher,
            new PTKD.Infrastructure.Security.Authentication.InternalProviderSubjectNormalizer(),
            new SecurityStampSessionInvalidationService(),
            _harness.Clock,
            _harness.Policy);
        var result = await service.AuthenticateAsync(new AuthenticateAccountCommand(
            "OIDC",
            "external-dummy-subject",
            "synthetic-external-passphrase"));

        Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, result.ErrorCode);
        Assert.Null(recordingHasher.LastVerifiedAccount);
        Assert.Null(recordingHasher.LastVerifiedHash);
    }

    [Fact]
    public async Task AdministratorUnlock_ClearsFailuresAndLockoutButPreservesPasswordTemporaryStateAndStamp()
    {
        var seed = await _harness.CreateInternalAccountAsync(
            "ADMIN-UNLOCK",
            "synthetic-current-passphrase",
            mustChangePassword: true,
            temporaryPasswordExpiresAt: _harness.Clock.UtcNow.AddHours(24));
        for (var attempt = 0; attempt < 5; attempt++)
            await AuthenticateAsync("ADMIN-UNLOCK", "synthetic-wrong-passphrase");

        var locked = await _harness.LoadAccountAsync(seed.AccountId);
        var result = await _harness.Service.AdministratorUnlockAsync(new AdministratorUnlockAccountCommand(
            seed.AccountId,
            locked.RowVersion,
            seed.UserId));
        var account = await _harness.LoadAccountAsync(seed.AccountId);

        Assert.True(result.Succeeded);
        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
        Assert.Equal(0, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
        Assert.Equal(locked.PasswordHash, account.PasswordHash);
        Assert.True(account.MustChangePassword);
        Assert.Equal(locked.TemporaryPasswordExpiresAt, account.TemporaryPasswordExpiresAt);
        Assert.Equal(locked.SecurityStamp, account.SecurityStamp);
    }

    [Fact]
    public async Task DisableAccount_PersistsSuspensionAndRotatesSecurityStampWithoutFakeSessionStore()
    {
        var seed = await _harness.CreateInternalAccountAsync("DISABLE-USER", "synthetic-passphrase");

        var result = await _harness.Service.DisableAccountAsync(new DisableAuthenticationAccountCommand(
            seed.AccountId,
            seed.RowVersion,
            seed.UserId));
        var account = await _harness.LoadAccountAsync(seed.AccountId);
        var authenticationResult = await AuthenticateAsync("DISABLE-USER", "synthetic-passphrase");

        Assert.True(result.Succeeded);
        Assert.Equal(AuthenticationAccountPolicy.DisabledAccountStatus, account.AuthAccountStatus);
        Assert.NotEqual(seed.SecurityStamp, account.SecurityStamp);
        Assert.Equal(_harness.Clock.UtcNow, account.SessionsInvalidatedAt);
        Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, authenticationResult.ErrorCode);
        Assert.Equal(0, account.FailedAttemptCount);

        await using var context = _harness.CreateContext();
        Assert.DoesNotContain(context.Model.GetEntityTypes(), type => type.GetTableName() == "Refresh_Tokens");
    }

    [Fact]
    public async Task StaleRowVersion_MapsToAcceptedConcurrencyConflictWithoutOverwrite()
    {
        var seed = await _harness.CreateInternalAccountAsync("STALE-VERSION", "synthetic-passphrase");
        await AuthenticateAsync("STALE-VERSION", "synthetic-wrong-passphrase");

        var result = await _harness.Service.AdministratorUnlockAsync(new AdministratorUnlockAccountCommand(
            seed.AccountId,
            seed.RowVersion,
            seed.UserId));
        var account = await _harness.LoadAccountAsync(seed.AccountId);

        Assert.False(result.Succeeded);
        Assert.Equal(AuthenticationErrorCodes.AccountConcurrencyConflict, result.ErrorCode);
        Assert.Equal(1, account.FailedAttemptCount);
    }

    [Fact]
    public async Task FiveConcurrentFailedAttempts_DoNotLoseIncrements()
    {
        var seed = await _harness.CreateInternalAccountAsync("CONCURRENT-FAIL", "synthetic-correct-passphrase");

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            AuthenticateAsync("CONCURRENT-FAIL", "synthetic-wrong-passphrase")));
        var account = await _harness.LoadAccountAsync(seed.AccountId);

        Assert.All(results, result => Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, result.ErrorCode));
        Assert.Equal(5, account.FailedAttemptCount);
        Assert.Equal(AuthenticationAccountPolicy.LockedAccountStatus, account.AuthAccountStatus);
        Assert.Equal(_harness.Clock.UtcNow.AddMinutes(15), account.LockoutEnd);
    }

    [Fact]
    public async Task CompetingPasswordChanges_ProduceOneSuccessAndOneDeterministicConflict()
    {
        var seed = await _harness.CreateInternalAccountAsync("CONCURRENT-CHANGE", "synthetic-current-passphrase");

        var results = await Task.WhenAll(
            _harness.Service.ChangePasswordAsync(new ChangePasswordCommand(
                seed.AccountId,
                "synthetic-current-passphrase",
                "synthetic-replacement-alpha",
                seed.RowVersion,
                seed.UserId)),
            _harness.Service.ChangePasswordAsync(new ChangePasswordCommand(
                seed.AccountId,
                "synthetic-current-passphrase",
                "synthetic-replacement-beta",
                seed.RowVersion,
                seed.UserId)));

        Assert.Single(results, result => result.Succeeded);
        Assert.Single(results, result => result.ErrorCode == AuthenticationErrorCodes.AccountConcurrencyConflict);
        Assert.Equal(1, await _harness.CountHistoryAsync(seed.AccountId));
    }

    [Fact]
    public async Task FailedAdministratorReset_RollsBackAccountHistoryAndStamp()
    {
        var seed = await _harness.CreateInternalAccountAsync("ROLLBACK-RESET", "synthetic-current-passphrase");

        var result = await _harness.Service.AdministratorResetPasswordAsync(new AdministratorResetPasswordCommand(
            seed.AccountId,
            "synthetic-temporary-passphrase",
            seed.RowVersion,
            long.MaxValue));

        var account = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.False(result.Succeeded);
        Assert.Equal(AuthenticationErrorCodes.UnexpectedDatabaseError, result.ErrorCode);
        Assert.Equal(seed.PasswordHash, account.PasswordHash);
        Assert.Equal(seed.SecurityStamp, account.SecurityStamp);
        Assert.True(seed.RowVersion.SequenceEqual(account.RowVersion));
        Assert.False(account.MustChangePassword);
        Assert.Null(account.TemporaryPasswordExpiresAt);
        Assert.Equal(0, await _harness.CountHistoryAsync(seed.AccountId));
    }

    [Fact]
    public async Task FailedPasswordChange_AppendsNoHistory()
    {
        var seed = await _harness.CreateInternalAccountAsync("FAILED-CHANGE", "synthetic-current-passphrase");

        var result = await _harness.Service.ChangePasswordAsync(new ChangePasswordCommand(
            seed.AccountId,
            "synthetic-wrong-passphrase",
            "synthetic-replacement-passphrase",
            seed.RowVersion,
            seed.UserId));

        var account = await _harness.LoadAccountAsync(seed.AccountId);
        Assert.False(result.Succeeded);
        Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, result.ErrorCode);
        Assert.Equal(seed.PasswordHash, account.PasswordHash);
        Assert.Equal(seed.SecurityStamp, account.SecurityStamp);
        Assert.True(seed.RowVersion.SequenceEqual(account.RowVersion));
        Assert.Equal(0, await _harness.CountHistoryAsync(seed.AccountId));
    }

    [Fact]
    public async Task TemporaryPassword_IsAcceptedBeforeExpiryAndRejectedAtAndAfterExpiry()
    {
        var beforeSeed = await _harness.CreateInternalAccountAsync(
            "TEMP-BEFORE",
            "synthetic-temporary-passphrase",
            mustChangePassword: true,
            temporaryPasswordExpiresAt: _harness.Clock.UtcNow.AddHours(24));
        _harness.Clock.UtcNow = _harness.Clock.UtcNow.AddHours(24).AddTicks(-1);

        var before = await AuthenticateAsync("TEMP-BEFORE", "synthetic-temporary-passphrase");
        Assert.Equal(AuthenticationAttemptOutcome.PasswordChangeRequired, before.Outcome);

        _harness.Clock.UtcNow = _harness.Clock.UtcNow.AddTicks(1);
        var atExpiry = await AuthenticateAsync("TEMP-BEFORE", "synthetic-temporary-passphrase");
        _harness.Clock.UtcNow = _harness.Clock.UtcNow.AddTicks(1);
        var afterExpiry = await AuthenticateAsync("TEMP-BEFORE", "synthetic-temporary-passphrase");

        Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, atExpiry.ErrorCode);
        Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, afterExpiry.ErrorCode);
        Assert.Equal(0, (await _harness.LoadAccountAsync(beforeSeed.AccountId)).FailedAttemptCount);
    }

    private Task<AuthenticationAttemptResult> AuthenticateAsync(string subject, string password) =>
        _harness.Service.AuthenticateAsync(new AuthenticateAccountCommand("INTERNAL", subject, password));
}

internal sealed class RecordingPasswordHashService : IPasswordHashService
{
    private readonly IPasswordHashService _inner;

    public RecordingPasswordHashService(IPasswordHashService inner)
    {
        _inner = inner;
    }

    public UserAuthAccount? LastVerifiedAccount { get; private set; }
    public string? LastVerifiedHash { get; private set; }

    public string HashPassword(UserAuthAccount account, string password) =>
        _inner.HashPassword(account, password);

    public PasswordHashVerificationResult VerifyPassword(
        UserAuthAccount? account,
        string? passwordHash,
        string suppliedPassword)
    {
        LastVerifiedAccount = account;
        LastVerifiedHash = passwordHash;
        return _inner.VerifyPassword(account, passwordHash, suppliedPassword);
    }
}
