using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Security.Authentication;

public sealed class AspNetCorePasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<UserAuthAccount> _passwordHasher;
    private readonly UserAuthAccount _dummyAccount;
    private readonly string _dummyHash;

    public AspNetCorePasswordHashService(PasswordHasher<UserAuthAccount>? passwordHasher = null)
    {
        _passwordHasher = passwordHasher ?? new PasswordHasher<UserAuthAccount>();
        _dummyAccount = UserAuthAccount.CreateInternal(
            long.MaxValue,
            "DUMMY",
            "INITIALIZATION_ONLY",
            DateTime.UnixEpoch);

        var dummySecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _dummyHash = _passwordHasher.HashPassword(_dummyAccount, dummySecret);
    }

    public string HashPassword(UserAuthAccount account, string password)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(password);
        if (!account.IsInternalProvider)
            throw new InvalidOperationException("External-provider accounts do not support local password hashing.");

        return _passwordHasher.HashPassword(account, password);
    }

    public PasswordHashVerificationResult VerifyPassword(
        UserAuthAccount? account,
        string? passwordHash,
        string suppliedPassword)
    {
        ArgumentNullException.ThrowIfNull(suppliedPassword);

        var effectiveAccount = account is not null && passwordHash is not null
            ? account
            : _dummyAccount;
        var effectiveHash = account is not null && passwordHash is not null
            ? passwordHash
            : _dummyHash;

        return _passwordHasher.VerifyHashedPassword(effectiveAccount, effectiveHash, suppliedPassword) switch
        {
            PasswordVerificationResult.Success => PasswordHashVerificationResult.Succeeded,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordHashVerificationResult.SucceededRehashNeeded,
            _ => PasswordHashVerificationResult.Failed
        };
    }
}
