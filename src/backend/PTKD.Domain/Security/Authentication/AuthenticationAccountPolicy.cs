using PTKD.Domain.Entities;

namespace PTKD.Domain.Security.Authentication;

public sealed class AuthenticationAccountPolicy
{
    public const string InternalProviderType = "INTERNAL";
    public const string ActiveAccountStatus = "ACTIVE";
    public const string LockedAccountStatus = "LOCKED";
    public const string DisabledAccountStatus = "DISABLED";

    public const string PasswordLengthInvalidCode = "AUTH_PASSWORD_LENGTH_INVALID";
    public const string PasswordContainsProviderSubjectCode = "AUTH_PASSWORD_CONTAINS_PROVIDER_SUBJECT";

    public AuthenticationAccountPolicy(
        int minimumPasswordLength = 8,
        int maximumPasswordLength = 64,
        int passwordHistoryDepth = 5,
        int maximumFailedAttempts = 5,
        TimeSpan? lockoutDuration = null,
        TimeSpan? temporaryPasswordLifetime = null)
    {
        if (minimumPasswordLength < 8)
            throw new ArgumentOutOfRangeException(nameof(minimumPasswordLength));
        if (maximumPasswordLength < minimumPasswordLength)
            throw new ArgumentOutOfRangeException(nameof(maximumPasswordLength));
        if (passwordHistoryDepth < 5)
            throw new ArgumentOutOfRangeException(nameof(passwordHistoryDepth));
        if (maximumFailedAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumFailedAttempts));

        var effectiveLockoutDuration = lockoutDuration ?? TimeSpan.FromMinutes(15);
        var effectiveTemporaryPasswordLifetime = temporaryPasswordLifetime ?? TimeSpan.FromHours(24);

        if (effectiveLockoutDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lockoutDuration));
        if (effectiveTemporaryPasswordLifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(temporaryPasswordLifetime));

        MinimumPasswordLength = minimumPasswordLength;
        MaximumPasswordLength = maximumPasswordLength;
        PasswordHistoryDepth = passwordHistoryDepth;
        MaximumFailedAttempts = maximumFailedAttempts;
        LockoutDuration = effectiveLockoutDuration;
        TemporaryPasswordLifetime = effectiveTemporaryPasswordLifetime;
    }

    public int MinimumPasswordLength { get; }
    public int MaximumPasswordLength { get; }
    public int PasswordHistoryDepth { get; }
    public int MaximumFailedAttempts { get; }
    public TimeSpan LockoutDuration { get; }
    public TimeSpan TemporaryPasswordLifetime { get; }

    public PasswordPolicyValidationResult ValidatePassword(string? password, string normalizedProviderSubject)
    {
        if (password is null
            || password.Length < MinimumPasswordLength
            || password.Length > MaximumPasswordLength)
        {
            return PasswordPolicyValidationResult.Failure(PasswordLengthInvalidCode);
        }

        if (!string.IsNullOrEmpty(normalizedProviderSubject)
            && password.Contains(normalizedProviderSubject, StringComparison.OrdinalIgnoreCase))
        {
            return PasswordPolicyValidationResult.Failure(PasswordContainsProviderSubjectCode);
        }

        return PasswordPolicyValidationResult.Success();
    }

    public bool IsLinkedUserEligible(User? user)
    {
        if (user is null)
            return false;

        return string.Equals(user.AccountStatus, ActiveAccountStatus, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(user.EmploymentStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.EmploymentStatus, "PROBATION", StringComparison.OrdinalIgnoreCase));
    }

    public bool IsTemporaryPasswordExpired(UserAuthAccount account, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(account);
        EnsureUtc(utcNow);

        return account.MustChangePassword
            && account.TemporaryPasswordExpiresAt.HasValue
            && utcNow >= account.TemporaryPasswordExpiresAt.Value;
    }

    internal static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Authentication lifecycle timestamps must be UTC.", nameof(value));
    }
}

public readonly record struct PasswordPolicyValidationResult(bool IsValid, string? ErrorCode)
{
    public static PasswordPolicyValidationResult Success() => new(true, null);

    public static PasswordPolicyValidationResult Failure(string errorCode) => new(false, errorCode);
}
