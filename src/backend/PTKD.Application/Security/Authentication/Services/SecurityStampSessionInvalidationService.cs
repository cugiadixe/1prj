using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Application.Security.Authentication.Services;

public sealed class SecurityStampSessionInvalidationService : ISessionInvalidationService
{
    public void Invalidate(UserAuthAccount account, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(account);
        account.InvalidateSessions(Guid.NewGuid(), utcNow);
    }
}
