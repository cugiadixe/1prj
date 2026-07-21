using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PTKD.Application.Security.AccountManagement;
using PTKD.Application.Security.AccountManagement.DTOs;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;

namespace PTKD.Infrastructure.Security.AccountManagement;

public sealed class AccountManagementService : IAccountManagementService
{
    private const string EntityType = "AUTH_ACCOUNT";
    private const string OutcomeSuccess = "SUCCESS";
    private const string OutcomeFailure = "FAILURE";

    private readonly IAuthenticationDbContextFactory _dbContextFactory;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ISessionInvalidationService _sessionInvalidationService;
    private readonly ITransactionalAuditWriter _transactionalAuditWriter;
    private readonly IUtcClock _clock;
    private readonly AuthenticationAccountPolicy _policy;

    public AccountManagementService(
        IAuthenticationDbContextFactory dbContextFactory,
        IPasswordHashService passwordHashService,
        ISessionInvalidationService sessionInvalidationService,
        ITransactionalAuditWriter transactionalAuditWriter,
        IUtcClock clock,
        AuthenticationAccountPolicy policy)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _passwordHashService = passwordHashService ?? throw new ArgumentNullException(nameof(passwordHashService));
        _sessionInvalidationService = sessionInvalidationService ?? throw new ArgumentNullException(nameof(sessionInvalidationService));
        _transactionalAuditWriter = transactionalAuditWriter ?? throw new ArgumentNullException(nameof(transactionalAuditWriter));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task<AccountDetailDto?> GetAccountDetailAsync(
        long accountId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var account = await context.UserAuthAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);

        if (account is null) return null;

        return new AccountDetailDto
        {
            Id = account.Id,
            UserId = account.UserId,
            ProviderType = account.ProviderType,
            Username = account.ProviderSubject,
            Status = account.AuthAccountStatus,
            IsInternalProvider = account.IsInternalProvider,
            FailedAttemptCount = account.FailedAttemptCount,
            IsManualLock = account.IsManualLock,
            LockoutEnd = account.LockoutEnd,
            MustChangePassword = account.MustChangePassword,
            TemporaryPasswordExpiresAt = account.TemporaryPasswordExpiresAt,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    public async Task<AccountManagementResult> ActivateAccountAsync(
        long accountId,
        long actingUserId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
        {
            var account = await context.FindAccountByIdForUpdateAsync(accountId, token);
            if (account is null)
                return (AccountManagementResult.Failure("AUTH_ACCOUNT_NOT_FOUND"), null);

            account.Activate(utcNow, actingUserId);
            await context.SaveChangesAsync(token);

            var audit = BuildAudit("ACCOUNT_ACTIVATED", account, actingUserId, reason: null, OutcomeSuccess);
            return (AccountManagementResult.Success(), audit);
        }, cancellationToken);
    }

    public async Task<AccountManagementResult> DisableAccountAsync(
        long accountId,
        string reason,
        long actingUserId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
        {
            var account = await context.FindAccountByIdForUpdateAsync(accountId, token);
            if (account is null)
                return (AccountManagementResult.Failure("AUTH_ACCOUNT_NOT_FOUND"), null);

            if (!account.Disable(utcNow, actingUserId))
                return (AccountManagementResult.Failure("AUTH_ACCOUNT_STATE_CONFLICT"), null);

            _sessionInvalidationService.Invalidate(account, utcNow);
            await context.SaveChangesAsync(token);

            var audit = BuildAudit("ACCOUNT_DISABLED", account, actingUserId, reason, OutcomeSuccess);
            return (AccountManagementResult.Success(), audit);
        }, cancellationToken);
    }

    public async Task<AccountManagementResult> LockAccountAsync(
        long accountId,
        string reason,
        long actingUserId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
        {
            var account = await context.FindAccountByIdForUpdateAsync(accountId, token);
            if (account is null)
                return (AccountManagementResult.Failure("AUTH_ACCOUNT_NOT_FOUND"), null);

            try
            {
                account.Lock(utcNow, actingUserId);
            }
            catch (InvalidOperationException)
            {
                return (AccountManagementResult.Failure("AUTH_ACCOUNT_STATE_CONFLICT"), null);
            }

            await context.SaveChangesAsync(token);

            var audit = BuildAudit("ACCOUNT_LOCKED", account, actingUserId, reason, OutcomeSuccess);
            return (AccountManagementResult.Success(), audit);
        }, cancellationToken);
    }

    public async Task<AccountManagementResult> UnlockAccountAsync(
        long accountId,
        long actingUserId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
        {
            var account = await context.FindAccountByIdForUpdateAsync(accountId, token);
            if (account is null)
                return (AccountManagementResult.Failure("AUTH_ACCOUNT_NOT_FOUND"), null);

            try
            {
                account.Unlock(utcNow, actingUserId);
            }
            catch (InvalidOperationException)
            {
                return (AccountManagementResult.Failure("AUTH_ACCOUNT_STATE_CONFLICT"), null);
            }

            await context.SaveChangesAsync(token);

            var audit = BuildAudit("ACCOUNT_UNLOCKED", account, actingUserId, reason: null, OutcomeSuccess);
            return (AccountManagementResult.Success(), audit);
        }, cancellationToken);
    }

    public async Task<AccountManagementResult> AdminResetPasswordAsync(
        long accountId,
        string reason,
        long actingUserId,
        CancellationToken cancellationToken = default)
    {
        // Generate temporary password before the transaction (crypto random, constant-length).
        // Never log this value.
        var temporaryPassword = GenerateTemporaryPassword();

        return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
        {
            var account = await context.FindAccountByIdForUpdateAsync(accountId, token);
            if (account is null)
                return (AccountManagementResult.Failure("AUTH_ACCOUNT_NOT_FOUND"), null);

            if (!account.IsInternalProvider || account.PasswordHash is null)
                return (AccountManagementResult.Failure("AUTH_EXTERNAL_PASSWORD_MANAGED"), null);

            var validation = _policy.ValidatePassword(temporaryPassword, account.ProviderSubject);
            if (!validation.IsValid)
                return (AccountManagementResult.Failure(validation.ErrorCode!), null);

            var histories = await context.GetRecentPasswordHistoryAsync(account.Id, _policy.PasswordHistoryDepth, token);
            if (IsPasswordReused(account, temporaryPassword, histories))
                return (AccountManagementResult.Failure("AUTH_PASSWORD_REUSE"), null);

            var outgoingHash = account.PasswordHash;
            var temporaryHash = _passwordHashService.HashPassword(account, temporaryPassword);
            context.PasswordHistories.Add(new PasswordHistory(account.Id, outgoingHash, utcNow));
            account.ReplacePassword(temporaryHash, true, utcNow.Add(_policy.TemporaryPasswordLifetime), utcNow, actingUserId);
            _sessionInvalidationService.Invalidate(account, utcNow);

            await context.SaveChangesAsync(token);

            // Audit must NOT contain the temporary password (SEC-005, DEC-1B-I-03).
            var audit = BuildAudit("ACCOUNT_PASSWORD_RESET_BY_ADMIN", account, actingUserId, reason, OutcomeSuccess);
            return (AccountManagementResult.SuccessWithPassword(temporaryPassword), audit);
        }, cancellationToken);
    }

    public async Task<AccountManagementResult> RevokeAllSessionsAsync(
        long accountId,
        string reason,
        long actingUserId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(async (context, utcNow, token) =>
        {
            var account = await context.FindAccountByIdForUpdateAsync(accountId, token);
            if (account is null)
                return (AccountManagementResult.Failure("AUTH_ACCOUNT_NOT_FOUND"), null);

            _sessionInvalidationService.Invalidate(account, utcNow);
            await context.SaveChangesAsync(token);

            var audit = BuildAudit("ACCOUNT_SESSIONS_REVOKED", account, actingUserId, reason, OutcomeSuccess);
            return (AccountManagementResult.Success(), audit);
        }, cancellationToken);
    }

    // Generates a cryptographically secure temporary password satisfying the default policy
    // (minimum 8 chars, no username constraint pre-checked here — validated in caller).
    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        const int length = 20;
        var buf = new char[length];
        var bytes = RandomNumberGenerator.GetBytes(length * 4);

        // Guarantee at least one of each character class.
        buf[0] = upper[bytes[0] % upper.Length];
        buf[1] = lower[bytes[4] % lower.Length];
        buf[2] = digits[bytes[8] % digits.Length];
        buf[3] = special[bytes[12] % special.Length];

        for (var i = 4; i < length; i++)
        {
            var idx = (int)(BitConverter.ToUInt32(bytes, i * 4) % (uint)all.Length);
            buf[i] = all[idx];
        }

        // Fisher-Yates shuffle to remove fixed positions.
        var shuffleBytes = RandomNumberGenerator.GetBytes(length);
        for (var i = length - 1; i > 0; i--)
        {
            var j = shuffleBytes[i] % (i + 1);
            (buf[i], buf[j]) = (buf[j], buf[i]);
        }

        return new string(buf);
    }

    private bool IsPasswordReused(UserAuthAccount account, string candidate, IReadOnlyList<PasswordHistory> histories)
    {
        if (_passwordHashService.VerifyPassword(account, account.PasswordHash, candidate) != PasswordHashVerificationResult.Failed)
            return true;

        return histories.Any(h =>
            _passwordHashService.VerifyPassword(account, h.PasswordHash, candidate) != PasswordHashVerificationResult.Failed);
    }

    private static SecurityAuditEventRecord BuildAudit(
        string eventCode,
        UserAuthAccount account,
        long actorUserId,
        string? reason,
        string outcome)
    {
        return new SecurityAuditEventRecord
        {
            EventCode = eventCode,
            EntityType = EntityType,
            EntityId = account.Id.ToString(),
            Outcome = outcome,
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            TargetUserId = account.UserId,
            Reason = reason
        };
    }

    private async Task<AccountManagementResult> ExecuteInTransactionAsync(
        Func<IAuthenticationDbContext, DateTime, CancellationToken, Task<(AccountManagementResult result, SecurityAuditEventRecord? audit)>> operation,
        CancellationToken cancellationToken)
    {
        await using var strategyContext = _dbContextFactory.CreateDbContext();
        var strategy = strategyContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var (result, audit) = await operation(context, _clock.UtcNow, cancellationToken);

            if (result.Succeeded && audit is not null)
            {
                audit.ThrowIfContainsSensitiveData();
                var dbConnection = context.GetDbConnection();
                var dbTransaction = context.GetCurrentDbTransaction()
                    ?? throw new InvalidOperationException(
                        "AccountManagementService: no active transaction for audit write.");
                await _transactionalAuditWriter.WriteAsync(audit, dbConnection, dbTransaction, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
