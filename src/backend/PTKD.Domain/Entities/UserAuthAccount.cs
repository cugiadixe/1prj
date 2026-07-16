using PTKD.Domain.Security.Authentication;

namespace PTKD.Domain.Entities;

public class UserAuthAccount
{
    private UserAuthAccount()
    {
    }

    private UserAuthAccount(
        long userId,
        string providerType,
        string providerSubject,
        string? passwordHash,
        DateTime utcNow,
        long? createdByUserId)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));
        if (string.IsNullOrWhiteSpace(providerType) || providerType.Length > 30)
            throw new ArgumentException("Provider type is invalid.", nameof(providerType));
        if (string.IsNullOrWhiteSpace(providerSubject) || providerSubject.Length > 200)
            throw new ArgumentException("Provider subject is invalid.", nameof(providerSubject));

        AuthenticationAccountPolicy.EnsureUtc(utcNow);

        UserId = userId;
        ProviderType = providerType;
        ProviderSubject = providerSubject;
        PasswordHash = passwordHash;
        AuthAccountStatus = AuthenticationAccountPolicy.ActiveAccountStatus;
        FailedAttemptCount = 0;
        MustChangePassword = false;
        SecurityStamp = Guid.NewGuid();
        CreatedAt = utcNow;
        CreatedByUserId = createdByUserId;
    }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string ProviderType { get; private set; } = null!;
    public string ProviderSubject { get; private set; } = null!;
    public string? PasswordHash { get; private set; }
    public string AuthAccountStatus { get; private set; } = null!;
    public int FailedAttemptCount { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public bool MustChangePassword { get; private set; }
    public DateTime? TemporaryPasswordExpiresAt { get; private set; }
    public Guid SecurityStamp { get; private set; }
    public DateTime? SessionsInvalidatedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public User User { get; private set; } = null!;
    public ICollection<PasswordHistory> PasswordHistories { get; private set; } = new List<PasswordHistory>();

    public bool IsInternalProvider =>
        string.Equals(ProviderType, AuthenticationAccountPolicy.InternalProviderType, StringComparison.Ordinal);

    public bool IsManualLock =>
        string.Equals(AuthAccountStatus, AuthenticationAccountPolicy.LockedAccountStatus, StringComparison.Ordinal)
        && !LockoutEnd.HasValue;

    public static UserAuthAccount CreateInternal(
        long userId,
        string providerSubject,
        string passwordHash,
        DateTime utcNow,
        long? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("An internal account requires a password hash.", nameof(passwordHash));

        return new UserAuthAccount(
            userId,
            AuthenticationAccountPolicy.InternalProviderType,
            providerSubject,
            passwordHash,
            utcNow,
            createdByUserId);
    }

    public static UserAuthAccount CreateExternal(
        long userId,
        string providerType,
        string providerSubject,
        DateTime utcNow,
        long? createdByUserId = null)
    {
        if (string.Equals(providerType, AuthenticationAccountPolicy.InternalProviderType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Use the internal-account factory for INTERNAL accounts.", nameof(providerType));

        return new UserAuthAccount(userId, providerType, providerSubject, null, utcNow, createdByUserId);
    }

    public bool ApplyExpiredTimedLockout(DateTime utcNow)
    {
        AuthenticationAccountPolicy.EnsureUtc(utcNow);

        if (!string.Equals(AuthAccountStatus, AuthenticationAccountPolicy.LockedAccountStatus, StringComparison.Ordinal)
            || !LockoutEnd.HasValue
            || utcNow < LockoutEnd.Value)
        {
            return false;
        }

        AuthAccountStatus = AuthenticationAccountPolicy.ActiveAccountStatus;
        FailedAttemptCount = 0;
        LockoutEnd = null;
        MarkUpdated(utcNow, null);
        return true;
    }

    public bool IsTimedLockoutActive(DateTime utcNow)
    {
        AuthenticationAccountPolicy.EnsureUtc(utcNow);

        return string.Equals(AuthAccountStatus, AuthenticationAccountPolicy.LockedAccountStatus, StringComparison.Ordinal)
            && LockoutEnd.HasValue
            && utcNow < LockoutEnd.Value;
    }

    public void RecordFailedAttempt(DateTime utcNow, int maximumFailedAttempts, TimeSpan lockoutDuration)
    {
        AuthenticationAccountPolicy.EnsureUtc(utcNow);
        if (maximumFailedAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumFailedAttempts));
        if (lockoutDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
        if (!string.Equals(AuthAccountStatus, AuthenticationAccountPolicy.ActiveAccountStatus, StringComparison.Ordinal))
            throw new InvalidOperationException("Only an active authentication account can record a failed attempt.");

        FailedAttemptCount = Math.Min(FailedAttemptCount + 1, maximumFailedAttempts);
        if (FailedAttemptCount >= maximumFailedAttempts)
        {
            AuthAccountStatus = AuthenticationAccountPolicy.LockedAccountStatus;
            LockoutEnd = utcNow.Add(lockoutDuration);
        }

        MarkUpdated(utcNow, null);
    }

    public void ResetAfterSuccessfulAuthentication(DateTime utcNow)
    {
        AuthenticationAccountPolicy.EnsureUtc(utcNow);
        AuthAccountStatus = AuthenticationAccountPolicy.ActiveAccountStatus;
        FailedAttemptCount = 0;
        LockoutEnd = null;
        MarkUpdated(utcNow, UserId);
    }

    public void RehashPassword(string newPasswordHash, DateTime utcNow)
    {
        EnsureInternalPasswordHash(newPasswordHash);
        AuthenticationAccountPolicy.EnsureUtc(utcNow);
        PasswordHash = newPasswordHash;
        MarkUpdated(utcNow, UserId);
    }

    public void ReplacePassword(
        string newPasswordHash,
        bool mustChangePassword,
        DateTime? temporaryPasswordExpiresAt,
        DateTime utcNow,
        long? updatedByUserId)
    {
        EnsureInternalPasswordHash(newPasswordHash);
        AuthenticationAccountPolicy.EnsureUtc(utcNow);
        if (temporaryPasswordExpiresAt.HasValue)
            AuthenticationAccountPolicy.EnsureUtc(temporaryPasswordExpiresAt.Value);
        if (temporaryPasswordExpiresAt.HasValue && !mustChangePassword)
            throw new ArgumentException("A temporary-password expiry requires must-change-password state.");

        PasswordHash = newPasswordHash;
        MustChangePassword = mustChangePassword;
        TemporaryPasswordExpiresAt = temporaryPasswordExpiresAt;
        AuthAccountStatus = AuthenticationAccountPolicy.ActiveAccountStatus;
        FailedAttemptCount = 0;
        LockoutEnd = null;
        MarkUpdated(utcNow, updatedByUserId);
    }

    public void Unlock(DateTime utcNow, long updatedByUserId)
    {
        AuthenticationAccountPolicy.EnsureUtc(utcNow);
        if (string.Equals(AuthAccountStatus, AuthenticationAccountPolicy.DisabledAccountStatus, StringComparison.Ordinal))
            throw new InvalidOperationException("A disabled account cannot be unlocked.");

        AuthAccountStatus = AuthenticationAccountPolicy.ActiveAccountStatus;
        FailedAttemptCount = 0;
        LockoutEnd = null;
        MarkUpdated(utcNow, updatedByUserId);
    }

    public bool Disable(DateTime utcNow, long updatedByUserId)
    {
        AuthenticationAccountPolicy.EnsureUtc(utcNow);
        if (string.Equals(AuthAccountStatus, AuthenticationAccountPolicy.DisabledAccountStatus, StringComparison.Ordinal))
            return false;

        AuthAccountStatus = AuthenticationAccountPolicy.DisabledAccountStatus;
        MarkUpdated(utcNow, updatedByUserId);
        return true;
    }

    public void InvalidateSessions(Guid newSecurityStamp, DateTime utcNow)
    {
        AuthenticationAccountPolicy.EnsureUtc(utcNow);
        if (newSecurityStamp == Guid.Empty)
            throw new ArgumentException("Security stamp must not be empty.", nameof(newSecurityStamp));
        if (newSecurityStamp == SecurityStamp)
            throw new ArgumentException("Security stamp must change.", nameof(newSecurityStamp));

        SecurityStamp = newSecurityStamp;
        if (!SessionsInvalidatedAt.HasValue || utcNow > SessionsInvalidatedAt.Value)
            SessionsInvalidatedAt = utcNow;

        MarkUpdated(utcNow, UpdatedByUserId);
    }

    private void EnsureInternalPasswordHash(string passwordHash)
    {
        if (!IsInternalProvider)
            throw new InvalidOperationException("External-provider accounts do not support local passwords.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        if (passwordHash.Length > 500)
            throw new ArgumentException("Password hash exceeds the accepted storage length.", nameof(passwordHash));
    }

    private void MarkUpdated(DateTime utcNow, long? updatedByUserId)
    {
        UpdatedAt = utcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
