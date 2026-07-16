using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Models;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;

namespace PTKD.Application.Security.Authentication.Services;

public sealed class AuthenticationAccountService : IAuthenticationAccountService
{
    private readonly IAuthenticationDbContextFactory _dbContextFactory;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IProviderSubjectNormalizer _providerSubjectNormalizer;
    private readonly ISessionInvalidationService _sessionInvalidationService;
    private readonly IUtcClock _clock;
    private readonly AuthenticationAccountPolicy _policy;

    public AuthenticationAccountService(
        IAuthenticationDbContextFactory dbContextFactory,
        IPasswordHashService passwordHashService,
        IProviderSubjectNormalizer providerSubjectNormalizer,
        ISessionInvalidationService sessionInvalidationService,
        IUtcClock clock,
        AuthenticationAccountPolicy policy)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _passwordHashService = passwordHashService ?? throw new ArgumentNullException(nameof(passwordHashService));
        _providerSubjectNormalizer = providerSubjectNormalizer ?? throw new ArgumentNullException(nameof(providerSubjectNormalizer));
        _sessionInvalidationService = sessionInvalidationService ?? throw new ArgumentNullException(nameof(sessionInvalidationService));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task<AuthenticationAttemptResult> AuthenticateAsync(
        AuthenticateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var suppliedPassword = command.Password ?? string.Empty;

        ProviderIdentity identity;
        try
        {
            identity = _providerSubjectNormalizer.Normalize(command.ProviderType, command.ProviderSubject);
        }
        catch (ArgumentException)
        {
            _passwordHashService.VerifyPassword(null, null, suppliedPassword);
            return AuthenticationAttemptResult.InvalidCredentials();
        }

        try
        {
            return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
            {
                var account = await context.FindAccountByProviderForUpdateAsync(
                    identity.ProviderType,
                    identity.ProviderSubject,
                    token);

                var hasUsableInternalHash = account is { IsInternalProvider: true, PasswordHash: not null };
                var verification = _passwordHashService.VerifyPassword(
                    hasUsableInternalHash ? account : null,
                    hasUsableInternalHash ? account!.PasswordHash : null,
                    suppliedPassword);

                if (account is null || !account.IsInternalProvider || account.PasswordHash is null)
                    return AuthenticationAttemptResult.InvalidCredentials();

                var expiredLockoutWasCleared = account.ApplyExpiredTimedLockout(utcNow);

                if (account.IsManualLock || account.IsTimedLockoutActive(utcNow))
                    return AuthenticationAttemptResult.InvalidCredentials();

                if (!string.Equals(
                        account.AuthAccountStatus,
                        AuthenticationAccountPolicy.ActiveAccountStatus,
                        StringComparison.Ordinal)
                    || !_policy.IsLinkedUserEligible(account.User))
                {
                    if (expiredLockoutWasCleared)
                        await context.SaveChangesAsync(token);

                    return AuthenticationAttemptResult.InvalidCredentials();
                }

                if (verification == PasswordHashVerificationResult.Failed)
                {
                    account.RecordFailedAttempt(
                        utcNow,
                        _policy.MaximumFailedAttempts,
                        _policy.LockoutDuration);
                    await context.SaveChangesAsync(token);
                    return AuthenticationAttemptResult.InvalidCredentials();
                }

                if (_policy.IsTemporaryPasswordExpired(account, utcNow))
                {
                    if (expiredLockoutWasCleared)
                        await context.SaveChangesAsync(token);

                    return AuthenticationAttemptResult.InvalidCredentials();
                }

                account.ResetAfterSuccessfulAuthentication(utcNow);

                if (verification == PasswordHashVerificationResult.SucceededRehashNeeded)
                {
                    var replacementHash = _passwordHashService.HashPassword(account, suppliedPassword);
                    account.RehashPassword(replacementHash, utcNow);
                }

                await context.SaveChangesAsync(token);
                return AuthenticationAttemptResult.Success(
                    account.Id,
                    account.UserId,
                    account.SecurityStamp,
                    account.RowVersion,
                    account.MustChangePassword);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AuthenticationAttemptResult.InfrastructureFailure();
        }
        catch (DbUpdateException)
        {
            return AuthenticationAttemptResult.InfrastructureFailure();
        }
        catch (RetryLimitExceededException)
        {
            return AuthenticationAttemptResult.InfrastructureFailure();
        }
        catch (DbException)
        {
            return AuthenticationAttemptResult.InfrastructureFailure();
        }
    }

    public async Task<AuthenticationAccountOperationResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
            {
                var account = await context.FindAccountByIdForUpdateAsync(command.AccountId, token);
                if (account is null)
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountNotFound);
                if (!HasTargetVersion(account, command.TargetRowVersion))
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountConcurrencyConflict);
                if (!account.IsInternalProvider || account.PasswordHash is null)
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.ExternalPasswordManaged);
                if (command.ActingUserId != account.UserId
                    || !string.Equals(account.AuthAccountStatus, AuthenticationAccountPolicy.ActiveAccountStatus, StringComparison.Ordinal)
                    || !_policy.IsLinkedUserEligible(account.User)
                    || _policy.IsTemporaryPasswordExpired(account, utcNow))
                {
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.InvalidCredentials);
                }

                var currentVerification = _passwordHashService.VerifyPassword(
                    account,
                    account.PasswordHash,
                    command.CurrentPassword ?? string.Empty);
                if (currentVerification == PasswordHashVerificationResult.Failed)
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.InvalidCredentials);

                var validation = _policy.ValidatePassword(command.NewPassword, account.ProviderSubject);
                if (!validation.IsValid)
                    return AuthenticationAccountOperationResult.Failure(validation.ErrorCode!);

                var histories = await context.GetRecentPasswordHistoryAsync(
                    account.Id,
                    _policy.PasswordHistoryDepth,
                    token);
                if (IsPasswordReused(account, command.NewPassword, histories))
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.PasswordReuse);

                var outgoingHash = account.PasswordHash;
                var replacementHash = _passwordHashService.HashPassword(account, command.NewPassword);
                context.PasswordHistories.Add(new PasswordHistory(account.Id, outgoingHash, utcNow));
                account.ReplacePassword(replacementHash, false, null, utcNow, command.ActingUserId);
                _sessionInvalidationService.Invalidate(account, utcNow);

                await context.SaveChangesAsync(token);
                return AuthenticationAccountOperationResult.Success(account.RowVersion);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
        catch (RetryLimitExceededException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
        catch (DbException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
    }

    public async Task<AuthenticationAccountOperationResult> AdministratorResetPasswordAsync(
        AdministratorResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
            {
                var account = await context.FindAccountByIdForUpdateAsync(command.AccountId, token);
                if (account is null)
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountNotFound);
                if (!HasTargetVersion(account, command.TargetRowVersion))
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountConcurrencyConflict);
                if (!account.IsInternalProvider || account.PasswordHash is null)
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.ExternalPasswordManaged);

                var validation = _policy.ValidatePassword(command.TemporaryPassword, account.ProviderSubject);
                if (!validation.IsValid)
                    return AuthenticationAccountOperationResult.Failure(validation.ErrorCode!);

                var histories = await context.GetRecentPasswordHistoryAsync(
                    account.Id,
                    _policy.PasswordHistoryDepth,
                    token);
                if (IsPasswordReused(account, command.TemporaryPassword, histories))
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.PasswordReuse);

                var outgoingHash = account.PasswordHash;
                var temporaryHash = _passwordHashService.HashPassword(account, command.TemporaryPassword);
                context.PasswordHistories.Add(new PasswordHistory(account.Id, outgoingHash, utcNow));
                account.ReplacePassword(
                    temporaryHash,
                    true,
                    utcNow.Add(_policy.TemporaryPasswordLifetime),
                    utcNow,
                    command.ActingUserId);
                _sessionInvalidationService.Invalidate(account, utcNow);

                await context.SaveChangesAsync(token);
                return AuthenticationAccountOperationResult.Success(account.RowVersion);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
        catch (RetryLimitExceededException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
        catch (DbException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
    }

    public async Task<AuthenticationAccountOperationResult> AdministratorUnlockAsync(
        AdministratorUnlockAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
            {
                var account = await context.FindAccountByIdForUpdateAsync(command.AccountId, token);
                if (account is null)
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountNotFound);
                if (!HasTargetVersion(account, command.TargetRowVersion))
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountConcurrencyConflict);

                try
                {
                    account.Unlock(utcNow, command.ActingUserId);
                }
                catch (InvalidOperationException)
                {
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountStateConflict);
                }

                await context.SaveChangesAsync(token);
                return AuthenticationAccountOperationResult.Success(account.RowVersion);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
        catch (RetryLimitExceededException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
        catch (DbException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
    }

    public async Task<AuthenticationAccountOperationResult> DisableAccountAsync(
        DisableAuthenticationAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
            {
                var account = await context.FindAccountByIdForUpdateAsync(command.AccountId, token);
                if (account is null)
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountNotFound);
                if (!HasTargetVersion(account, command.TargetRowVersion))
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountConcurrencyConflict);
                if (!account.Disable(utcNow, command.ActingUserId))
                    return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountStateConflict);

                _sessionInvalidationService.Invalidate(account, utcNow);
                await context.SaveChangesAsync(token);
                return AuthenticationAccountOperationResult.Success(account.RowVersion);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.AccountConcurrencyConflict);
        }
        catch (DbUpdateException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
        catch (RetryLimitExceededException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
        catch (DbException)
        {
            return AuthenticationAccountOperationResult.Failure(AuthenticationErrorCodes.UnexpectedDatabaseError);
        }
    }

    private bool IsPasswordReused(
        UserAuthAccount account,
        string candidatePassword,
        IReadOnlyList<PasswordHistory> histories)
    {
        if (_passwordHashService.VerifyPassword(account, account.PasswordHash, candidatePassword)
            != PasswordHashVerificationResult.Failed)
        {
            return true;
        }

        return histories.Any(history =>
            _passwordHashService.VerifyPassword(account, history.PasswordHash, candidatePassword)
            != PasswordHashVerificationResult.Failed);
    }

    private static bool HasTargetVersion(UserAuthAccount account, byte[]? targetRowVersion)
    {
        return targetRowVersion is { Length: 8 }
            && account.RowVersion is { Length: 8 }
            && account.RowVersion.SequenceEqual(targetRowVersion);
    }

    private async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IAuthenticationDbContext, DateTime, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        await using var strategyContext = _dbContextFactory.CreateDbContext();
        var strategy = strategyContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var result = await operation(context, _clock.UtcNow, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
