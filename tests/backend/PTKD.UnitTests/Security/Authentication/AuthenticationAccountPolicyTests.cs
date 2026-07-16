using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;

namespace PTKD.UnitTests.Security.Authentication;

public sealed class AuthenticationAccountPolicyTests
{
    private readonly AuthenticationAccountPolicy _policy = new();

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(64, true)]
    [InlineData(65, false)]
    public void PasswordLength_EnforcesAcceptedBoundaries(int length, bool expected)
    {
        var result = _policy.ValidatePassword(new string('a', length), "USER-IDENTIFIER");

        Assert.Equal(expected, result.IsValid);
        if (!expected)
            Assert.Equal(AuthenticationAccountPolicy.PasswordLengthInvalidCode, result.ErrorCode);
    }

    [Theory]
    [InlineData("BACHDH-extra")]
    [InlineData("prefix-bachdh-suffix")]
    [InlineData("prefix-BaChDh-suffix")]
    public void PasswordContainingCanonicalSubject_IsRejected(string candidate)
    {
        var result = _policy.ValidatePassword(candidate, "BACHDH");

        Assert.False(result.IsValid);
        Assert.Equal(AuthenticationAccountPolicy.PasswordContainsProviderSubjectCode, result.ErrorCode);
    }

    [Theory]
    [InlineData("aaaaaaaa")]
    [InlineData("abcdefgh")]
    [InlineData("!!!!!!!!")]
    public void PasswordComposition_AddsNoCharacterClassRequirement(string candidate)
    {
        Assert.True(_policy.ValidatePassword(candidate, "USER-IDENTIFIER").IsValid);
    }

    [Theory]
    [InlineData("ACTIVE", "ACTIVE", true)]
    [InlineData("active", "probation", true)]
    [InlineData("SUSPENDED", "ACTIVE", false)]
    [InlineData("ACTIVE", "SUSPENDED", false)]
    [InlineData("ACTIVE", "TERMINATED", false)]
    [InlineData("ACTIVE", "RETIRED", false)]
    [InlineData("ACTIVE", "RESIGNED", false)]
    [InlineData("ACTIVE", "INACTIVE", false)]
    [InlineData("UNKNOWN", "ACTIVE", false)]
    public void LinkedUserEligibility_FailsClosed(
        string accountStatus,
        string employmentStatus,
        bool expected)
    {
        var user = new User("EMP001", "Test User", null, employmentStatus, accountStatus);

        Assert.Equal(expected, _policy.IsLinkedUserEligible(user));
    }

    [Fact]
    public void TemporaryPassword_IsInvalidAtExactExpiryAndAfter()
    {
        var now = new DateTime(2026, 7, 16, 3, 0, 0, DateTimeKind.Utc);
        var account = UserAuthAccount.CreateInternal(1, "BACHDH", "hash", now);
        account.ReplacePassword("temp-hash", true, now.AddHours(24), now, 1);

        Assert.False(_policy.IsTemporaryPasswordExpired(account, now.AddHours(24).AddTicks(-1)));
        Assert.True(_policy.IsTemporaryPasswordExpired(account, now.AddHours(24)));
        Assert.True(_policy.IsTemporaryPasswordExpired(account, now.AddHours(24).AddTicks(1)));
    }

    [Fact]
    public void UnsafeConfiguration_FailsFast()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthenticationAccountPolicy(minimumPasswordLength: 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthenticationAccountPolicy(minimumPasswordLength: 10, maximumPasswordLength: 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthenticationAccountPolicy(passwordHistoryDepth: 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthenticationAccountPolicy(maximumFailedAttempts: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthenticationAccountPolicy(lockoutDuration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthenticationAccountPolicy(temporaryPasswordLifetime: TimeSpan.Zero));
    }
}
