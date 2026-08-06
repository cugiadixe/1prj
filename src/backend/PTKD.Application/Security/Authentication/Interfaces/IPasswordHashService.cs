using PTKD.Domain.Entities;

namespace PTKD.Application.Security.Authentication.Interfaces;

public interface IPasswordHashService
{
    string HashPassword(UserAuthAccount account, string password);

    PasswordHashVerificationResult VerifyPassword(
        UserAuthAccount? account,
        string? passwordHash,
        string suppliedPassword);
}

public enum PasswordHashVerificationResult
{
    Failed = 0,
    Succeeded = 1,
    SucceededRehashNeeded = 2
}
