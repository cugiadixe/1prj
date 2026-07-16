using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;

namespace PTKD.UnitTests.Security.Authentication;

public sealed class UserAuthAccountTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 16, 3, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void InternalAndExternalAccounts_EnforceHashBoundary()
    {
        var internalAccount = CreateInternal();
        var externalAccount = UserAuthAccount.CreateExternal(2, "OIDC", "opaque-subject", UtcNow);

        Assert.True(internalAccount.IsInternalProvider);
        Assert.NotNull(internalAccount.PasswordHash);
        Assert.False(externalAccount.IsInternalProvider);
        Assert.Null(externalAccount.PasswordHash);
        Assert.Throws<ArgumentException>(() =>
            UserAuthAccount.CreateInternal(3, "LOCAL", string.Empty, UtcNow));
        Assert.Throws<ArgumentException>(() =>
            UserAuthAccount.CreateExternal(3, "INTERNAL", "LOCAL", UtcNow));
    }

    [Fact]
    public void FailedAttempts_OneThroughFourRemainActive_FifthCreatesUtcLockout()
    {
        var account = CreateInternal();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));
            Assert.Equal(attempt, account.FailedAttemptCount);
            Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
            Assert.Null(account.LockoutEnd);
        }

        account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));

        Assert.Equal(5, account.FailedAttemptCount);
        Assert.Equal(AuthenticationAccountPolicy.LockedAccountStatus, account.AuthAccountStatus);
        Assert.Equal(UtcNow.AddMinutes(15), account.LockoutEnd);
        Assert.Equal(DateTimeKind.Utc, account.LockoutEnd!.Value.Kind);
    }

    [Fact]
    public void ExpiredLockout_ClearsThenCurrentFailureBecomesAttemptOne()
    {
        var account = CreateInternal();
        for (var attempt = 0; attempt < 5; attempt++)
            account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));

        var atExpiry = UtcNow.AddMinutes(15);
        Assert.True(account.ApplyExpiredTimedLockout(atExpiry));
        Assert.Equal(0, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);

        account.RecordFailedAttempt(atExpiry, 5, TimeSpan.FromMinutes(15));
        Assert.Equal(1, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
    }

    [Fact]
    public void SuccessfulAuthentication_ResetsFailureState()
    {
        var account = CreateInternal();
        account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));
        account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));

        account.ResetAfterSuccessfulAuthentication(UtcNow.AddSeconds(1));

        Assert.Equal(0, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
    }

    [Fact]
    public void AdministratorTemporaryReplacement_SetsExactLifecycleState()
    {
        var account = CreateInternal();
        account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));
        var expiresAt = UtcNow.AddHours(24);

        account.ReplacePassword("temporary-hash", true, expiresAt, UtcNow, 42);

        Assert.Equal("temporary-hash", account.PasswordHash);
        Assert.True(account.MustChangePassword);
        Assert.Equal(expiresAt, account.TemporaryPasswordExpiresAt);
        Assert.Equal(0, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
        Assert.Equal(42, account.UpdatedByUserId);
    }

    [Fact]
    public void Rehash_ChangesOnlyHashAndModificationMetadata()
    {
        var account = CreateInternal();
        account.ReplacePassword("temporary-hash", true, UtcNow.AddHours(24), UtcNow, 42);
        var stamp = account.SecurityStamp;
        var expiry = account.TemporaryPasswordExpiresAt;

        account.RehashPassword("rehash-value", UtcNow.AddMinutes(1));

        Assert.Equal("rehash-value", account.PasswordHash);
        Assert.Equal(stamp, account.SecurityStamp);
        Assert.True(account.MustChangePassword);
        Assert.Equal(expiry, account.TemporaryPasswordExpiresAt);
    }

    [Fact]
    public void DomainLifecycle_RejectsNonUtcTime()
    {
        var account = CreateInternal();
        var localTime = DateTime.SpecifyKind(UtcNow, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            account.RecordFailedAttempt(localTime, 5, TimeSpan.FromMinutes(15)));
    }

    private static UserAuthAccount CreateInternal() =>
        UserAuthAccount.CreateInternal(1, "BACHDH", "existing-hash", UtcNow);
}
