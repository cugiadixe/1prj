using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Models;
using PTKD.Application.Security.Authentication.Services;
using PTKD.Domain.Entities;
using PTKD.Infrastructure.Security.Authentication;

namespace PTKD.UnitTests.Security.Authentication;

public sealed class AuthenticationAccountServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 16, 3, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AspNetCoreHasher_ValidAndInvalidPasswordsMapCorrectly()
    {
        var service = new AspNetCorePasswordHashService();
        var account = CreateAccount();
        var hash = service.HashPassword(account, "synthetic-passphrase");

        Assert.Equal(
            PasswordHashVerificationResult.Succeeded,
            service.VerifyPassword(account, hash, "synthetic-passphrase"));
        Assert.Equal(
            PasswordHashVerificationResult.Failed,
            service.VerifyPassword(account, hash, "different-synthetic-passphrase"));
    }

    [Fact]
    public void AspNetCoreHasher_MapsLegacySuccessToRehashNeeded()
    {
        var account = CreateAccount();
        var legacyHasher = new PasswordHasher<UserAuthAccount>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2
        }));
        var legacyHash = legacyHasher.HashPassword(account, "synthetic-passphrase");
        var currentService = new AspNetCorePasswordHashService();

        Assert.Equal(
            PasswordHashVerificationResult.SucceededRehashNeeded,
            currentService.VerifyPassword(account, legacyHash, "synthetic-passphrase"));
    }

    [Fact]
    public void AspNetCoreHasher_UnknownAccountUsesDummyVerificationAndFails()
    {
        var service = new AspNetCorePasswordHashService();

        Assert.Equal(
            PasswordHashVerificationResult.Failed,
            service.VerifyPassword(null, null, "synthetic-passphrase"));
    }

    [Fact]
    public void AspNetCoreHasher_RejectsExternalLocalHashCreation()
    {
        var service = new AspNetCorePasswordHashService();
        var externalAccount = UserAuthAccount.CreateExternal(1, "OIDC", "opaque-subject", UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            service.HashPassword(externalAccount, "synthetic-passphrase"));
    }

    [Fact]
    public void ProviderNormalizer_CanonicalizesInternalAndPreservesExternalSubject()
    {
        var normalizer = new InternalProviderSubjectNormalizer();

        var internalIdentity = normalizer.Normalize(" internal ", " bachdh ");
        var externalIdentity = normalizer.Normalize("oidc", "Case-Sensitive-Subject");

        Assert.Equal(new ProviderIdentity("INTERNAL", "BACHDH"), internalIdentity);
        Assert.Equal(new ProviderIdentity("OIDC", "Case-Sensitive-Subject"), externalIdentity);
    }

    [Fact]
    public void NonEnumeratingFailures_HaveIdenticalSafeShape()
    {
        var unknown = AuthenticationAttemptResult.InvalidCredentials();
        var locked = AuthenticationAttemptResult.InvalidCredentials();
        var ineligible = AuthenticationAttemptResult.InvalidCredentials();

        Assert.Equal(unknown, locked);
        Assert.Equal(unknown, ineligible);
        Assert.Equal(AuthenticationErrorCodes.InvalidCredentials, unknown.ErrorCode);
        Assert.Null(unknown.AccountId);
        Assert.Null(unknown.UserId);
        Assert.Null(unknown.SecurityStamp);
        Assert.Null(unknown.RowVersion);
    }

    [Fact]
    public void AuthenticationAccountService_RejectsMissingDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new AuthenticationAccountService(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!));
    }

    private static UserAuthAccount CreateAccount() =>
        UserAuthAccount.CreateInternal(1, "BACHDH", "initial-hash", UtcNow);
}
