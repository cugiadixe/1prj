using PTKD.Domain.Entities;

namespace PTKD.Application.Security.Authentication.Interfaces;

public interface ISessionInvalidationService
{
    void Invalidate(UserAuthAccount account, DateTime utcNow);
}
