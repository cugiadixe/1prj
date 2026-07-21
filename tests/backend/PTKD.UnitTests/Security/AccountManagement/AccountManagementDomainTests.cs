using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;

namespace PTKD.UnitTests.Security.AccountManagement;

public sealed class AccountManagementDomainTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 21, 9, 0, 0, DateTimeKind.Utc);

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenDisabled_TransitionsToActive()
    {
        var account = CreateDisabledAccount();
        account.Activate(UtcNow, updatedByUserId: 99);

        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
        Assert.Equal(0, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
    }

    [Fact]
    public void Activate_WhenLocked_TransitionsToActive()
    {
        var account = CreateLockedAccount();
        account.Activate(UtcNow, updatedByUserId: 99);

        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
        Assert.Equal(0, account.FailedAttemptCount);
        Assert.Null(account.LockoutEnd);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsIdempotent()
    {
        var account = CreateActiveAccount();
        account.Activate(UtcNow, updatedByUserId: 99);

        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
    }

    [Fact]
    public void Activate_RequiresUtc()
    {
        var account = CreateDisabledAccount();
        var local = new DateTime(2026, 7, 21, 9, 0, 0, DateTimeKind.Local);
        Assert.Throws<ArgumentException>(() => account.Activate(local, updatedByUserId: 99));
    }

    // ── Lock ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Lock_WhenActive_SetsLockedStatusAndNoExpiry()
    {
        var account = CreateActiveAccount();
        account.Lock(UtcNow, updatedByUserId: 99);

        Assert.Equal(AuthenticationAccountPolicy.LockedAccountStatus, account.AuthAccountStatus);
        Assert.Null(account.LockoutEnd);
        Assert.True(account.IsManualLock);
    }

    [Fact]
    public void Lock_WhenAlreadyLocked_SetsLockedAgain()
    {
        var account = CreateLockedAccount();
        account.Lock(UtcNow, updatedByUserId: 99);

        Assert.Equal(AuthenticationAccountPolicy.LockedAccountStatus, account.AuthAccountStatus);
        Assert.Null(account.LockoutEnd);
    }

    [Fact]
    public void Lock_WhenDisabled_Throws()
    {
        var account = CreateDisabledAccount();
        Assert.Throws<InvalidOperationException>(() => account.Lock(UtcNow, updatedByUserId: 99));
    }

    [Fact]
    public void Lock_RequiresUtc()
    {
        var account = CreateActiveAccount();
        var local = new DateTime(2026, 7, 21, 9, 0, 0, DateTimeKind.Local);
        Assert.Throws<ArgumentException>(() => account.Lock(local, updatedByUserId: 99));
    }

    // ── Lock→Activate roundtrip ───────────────────────────────────────────────

    [Fact]
    public void LockThenActivate_TransitionsCorrectly()
    {
        var account = CreateActiveAccount();
        account.Lock(UtcNow, updatedByUserId: 99);
        Assert.Equal(AuthenticationAccountPolicy.LockedAccountStatus, account.AuthAccountStatus);

        account.Activate(UtcNow.AddMinutes(1), updatedByUserId: 99);
        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
    }

    // ── Disable → Activate roundtrip ─────────────────────────────────────────

    [Fact]
    public void DisableThenActivate_TransitionsCorrectly()
    {
        var account = CreateActiveAccount();
        account.Disable(UtcNow, updatedByUserId: 99);
        Assert.Equal(AuthenticationAccountPolicy.DisabledAccountStatus, account.AuthAccountStatus);

        account.Activate(UtcNow.AddMinutes(1), updatedByUserId: 99);
        Assert.Equal(AuthenticationAccountPolicy.ActiveAccountStatus, account.AuthAccountStatus);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UserAuthAccount CreateActiveAccount()
    {
        var account = UserAuthAccount.CreateInternal(1, "sub_active", "hash", UtcNow);
        return account;
    }

    private static UserAuthAccount CreateLockedAccount()
    {
        var account = UserAuthAccount.CreateInternal(1, "sub_locked", "hash", UtcNow);
        account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));
        account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));
        account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));
        account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));
        account.RecordFailedAttempt(UtcNow, 5, TimeSpan.FromMinutes(15));
        return account;
    }

    private static UserAuthAccount CreateDisabledAccount()
    {
        var account = UserAuthAccount.CreateInternal(1, "sub_disabled", "hash", UtcNow);
        account.Disable(UtcNow, updatedByUserId: 1);
        return account;
    }
}
