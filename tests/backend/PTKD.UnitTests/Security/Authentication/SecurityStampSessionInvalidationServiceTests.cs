using PTKD.Application.Security.Authentication.Services;
using PTKD.Domain.Entities;

namespace PTKD.UnitTests.Security.Authentication;

public sealed class SecurityStampSessionInvalidationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 16, 3, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Invalidate_ChangesStampAndPersistsUtcCutoff()
    {
        var account = CreateAccount();
        var originalStamp = account.SecurityStamp;
        var service = new SecurityStampSessionInvalidationService();

        service.Invalidate(account, UtcNow);

        Assert.NotEqual(originalStamp, account.SecurityStamp);
        Assert.NotEqual(Guid.Empty, account.SecurityStamp);
        Assert.Equal(UtcNow, account.SessionsInvalidatedAt);
    }

    [Fact]
    public void Invalidate_WithOlderTime_DoesNotMoveCutoffBackwardButStillRotatesStamp()
    {
        var account = CreateAccount();
        var service = new SecurityStampSessionInvalidationService();
        service.Invalidate(account, UtcNow);
        var firstStamp = account.SecurityStamp;

        service.Invalidate(account, UtcNow.AddMinutes(-1));

        Assert.NotEqual(firstStamp, account.SecurityStamp);
        Assert.Equal(UtcNow, account.SessionsInvalidatedAt);
    }

    [Fact]
    public void DisableThenInvalidate_IsARealPersistentStateMutation()
    {
        var account = CreateAccount();
        var originalStamp = account.SecurityStamp;
        var service = new SecurityStampSessionInvalidationService();

        Assert.True(account.Disable(UtcNow, 42));
        service.Invalidate(account, UtcNow);

        Assert.Equal("DISABLED", account.AuthAccountStatus);
        Assert.NotEqual(originalStamp, account.SecurityStamp);
        Assert.Equal(UtcNow, account.SessionsInvalidatedAt);
    }

    private static UserAuthAccount CreateAccount() =>
        UserAuthAccount.CreateInternal(1, "BACHDH", "initial-hash", UtcNow);
}
