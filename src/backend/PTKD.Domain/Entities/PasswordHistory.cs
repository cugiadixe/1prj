using PTKD.Domain.Security.Authentication;

namespace PTKD.Domain.Entities;

public class PasswordHistory
{
    private PasswordHistory()
    {
    }

    public PasswordHistory(long accountId, string passwordHash, DateTime utcNow)
    {
        if (accountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(accountId));
        if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length > 500)
            throw new ArgumentException("Password history hash is invalid.", nameof(passwordHash));

        AuthenticationAccountPolicy.EnsureUtc(utcNow);
        AccountId = accountId;
        PasswordHash = passwordHash;
        CreatedAt = utcNow;
    }

    public long Id { get; private set; }
    public long AccountId { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public UserAuthAccount Account { get; private set; } = null!;
}
