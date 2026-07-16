using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Models;
using PTKD.Domain.Security.Authentication;

namespace PTKD.Application.Security.Authentication.Services;

public sealed class TokenSessionLifecycleService : ITokenSessionLifecycleService
{
    private readonly ITokenSessionDbContextFactory _dbContextFactory;
    private readonly IJwtAccessTokenService _jwtAccessTokenService;
    private readonly IRefreshTokenMaterialService _refreshTokenMaterialService;
    private readonly TimeProvider _timeProvider;

    public TokenSessionLifecycleService(
        ITokenSessionDbContextFactory dbContextFactory,
        IJwtAccessTokenService jwtAccessTokenService,
        IRefreshTokenMaterialService refreshTokenMaterialService,
        TimeProvider timeProvider)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _jwtAccessTokenService = jwtAccessTokenService ?? throw new ArgumentNullException(nameof(jwtAccessTokenService));
        _refreshTokenMaterialService = refreshTokenMaterialService ?? throw new ArgumentNullException(nameof(refreshTokenMaterialService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<TokenSessionResult> CreateSessionAsync(
        long accountId,
        string username,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var familyId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        
        var (rawMaterial, tokenHash) = _refreshTokenMaterialService.Generate();
        var refreshTokenExpiresAt = utcNow.AddDays(7);

        var account = await dbContext.FindAccountByIdForUpdateAsync(accountId, cancellationToken);
        if (account == null)
            return TokenSessionResult.Failure(TokenSessionStatus.InvalidCredentials, "ACCOUNT_NOT_FOUND");
            
        if (string.Equals(account.AuthAccountStatus, "DISABLED", StringComparison.OrdinalIgnoreCase))
            return TokenSessionResult.Failure(TokenSessionStatus.AccountDisabled, "ACCOUNT_DISABLED");
            
        if (account.IsTimedLockoutActive(utcNow) || string.Equals(account.AuthAccountStatus, "LOCKED", StringComparison.OrdinalIgnoreCase))
            return TokenSessionResult.Failure(TokenSessionStatus.AccountLocked, "ACCOUNT_LOCKED");

        if (account.User == null)
            return TokenSessionResult.Failure(TokenSessionStatus.InvalidCredentials, "USER_NOT_FOUND");
            
        if (!string.Equals(account.User.EmploymentStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(account.User.EmploymentStatus, "PROBATION", StringComparison.OrdinalIgnoreCase))
            return TokenSessionResult.Failure(TokenSessionStatus.InvalidCredentials, "EMPLOYMENT_INELIGIBLE");

        var refreshToken = RefreshToken.CreateRoot(
            accountId,
            tokenHash,
            familyId,
            sessionId,
            utcNow,
            refreshTokenExpiresAt,
            ipAddress,
            userAgent);

        dbContext.AddRefreshToken(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var accessRequest = new AccessTokenRequest(
            account.UserId,
            account.Id,
            sessionId,
            familyId,
            account.SecurityStamp,
            username);

        var jwtResult = _jwtAccessTokenService.IssueAccessToken(accessRequest);

        return TokenSessionResult.Success(
            jwtResult.Token,
            jwtResult.ExpiresAtUtc,
            rawMaterial,
            refreshTokenExpiresAt);
    }

    public async Task<TokenSessionResult> RefreshSessionAsync(
        string refreshTokenMaterial,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenMaterial))
            return TokenSessionResult.Failure(TokenSessionStatus.RefreshTokenInvalid, "TOKEN_EMPTY");

        var tokenHash = _refreshTokenMaterialService.ComputeHash(refreshTokenMaterial);
        
        using var dbContext = _dbContextFactory.CreateDbContext();
        var executionStrategy = dbContext.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            using var transaction = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            
            var token = await dbContext.FindRefreshTokenByHashForUpdateAsync(tokenHash, cancellationToken);
            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

            if (token == null)
                return TokenSessionResult.Failure(TokenSessionStatus.RefreshTokenInvalid, "TOKEN_NOT_FOUND");
            
            if (token.IsExpired(utcNow))
                return TokenSessionResult.Failure(TokenSessionStatus.RefreshTokenInvalid, "TOKEN_EXPIRED");

            // Check IsUsed before IsRevoked: when a concurrent thread detects reuse
            // and revokes the family, the token becomes both used AND revoked.
            // Checking IsUsed first ensures correct TOKEN_REUSED semantics.
            if (token.IsUsed)
            {
                // Use raw SQL for both operations to avoid row_version conflicts:
                // RevokeFamilyAsync uses ExecuteUpdateAsync which changes row_version in DB,
                // so we cannot also update the same entity via EF change tracking.
                await dbContext.MarkReuseDetectedAsync(token.Id, utcNow, cancellationToken);
                await dbContext.RevokeFamilyAsync(token.FamilyId, RefreshToken.RevokeReasonReuseDetected, utcNow, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return TokenSessionResult.Failure(TokenSessionStatus.RefreshTokenReused, "TOKEN_REUSED");
            }

            if (token.IsRevoked)
                return TokenSessionResult.Failure(TokenSessionStatus.SessionRevoked, "TOKEN_REVOKED");

            var account = await dbContext.FindAccountByIdForUpdateAsync(token.AccountId, cancellationToken);
            if (account == null)
                return TokenSessionResult.Failure(TokenSessionStatus.RefreshTokenInvalid, "ACCOUNT_NOT_FOUND");
            
            if (string.Equals(account.AuthAccountStatus, "DISABLED", StringComparison.OrdinalIgnoreCase))
                return TokenSessionResult.Failure(TokenSessionStatus.AccountDisabled, "ACCOUNT_DISABLED");
                
            if (account.IsTimedLockoutActive(utcNow) || string.Equals(account.AuthAccountStatus, "LOCKED", StringComparison.OrdinalIgnoreCase))
                return TokenSessionResult.Failure(TokenSessionStatus.AccountLocked, "ACCOUNT_LOCKED");

            if (account.User == null)
                return TokenSessionResult.Failure(TokenSessionStatus.InvalidCredentials, "USER_NOT_FOUND");
            
            if (!string.Equals(account.User.EmploymentStatus, "ACTIVE", StringComparison.OrdinalIgnoreCase) && 
                !string.Equals(account.User.EmploymentStatus, "PROBATION", StringComparison.OrdinalIgnoreCase))
                return TokenSessionResult.Failure(TokenSessionStatus.InvalidCredentials, "EMPLOYMENT_INELIGIBLE");

            if (account.SessionsInvalidatedAt.HasValue && account.SessionsInvalidatedAt.Value > token.IssuedAt)
            {
                // Use only raw SQL bulk update; no EF tracked entity mutation.
                await dbContext.RevokeFamilyAsync(token.FamilyId, "SESSIONS_INVALIDATED", utcNow, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return TokenSessionResult.Failure(TokenSessionStatus.SessionRevoked, "SESSIONS_INVALIDATED_CUTOFF");
            }

            var (newRawMaterial, newHash) = _refreshTokenMaterialService.Generate();
            var newExpiresAt = utcNow.AddDays(7);
            
            var replacement = RefreshToken.CreateReplacement(
                account.Id,
                newHash,
                token.FamilyId,
                token.SessionId,
                utcNow,
                newExpiresAt,
                token.Id,
                ipAddress,
                userAgent);

            try
            {
                dbContext.AddRefreshToken(replacement);
                await dbContext.SaveChangesAsync(cancellationToken);
                
                token.MarkUsed(replacement.Id, utcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another concurrent thread already rotated this token.
                // Treat as reuse detection: revoke the entire family.
                // Must use a fresh context since the current one is in a faulted state.
                using var freshContext = _dbContextFactory.CreateDbContext();
                using var freshTx = await freshContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                await freshContext.MarkReuseDetectedAsync(token.Id, utcNow, cancellationToken);
                await freshContext.RevokeFamilyAsync(token.FamilyId, RefreshToken.RevokeReasonReuseDetected, utcNow, cancellationToken);
                await freshTx.CommitAsync(cancellationToken);
                return TokenSessionResult.Failure(TokenSessionStatus.RefreshTokenReused, "TOKEN_REUSED");
            }

            var accessRequest = new AccessTokenRequest(
                account.UserId,
                account.Id,
                token.SessionId,
                token.FamilyId,
                account.SecurityStamp,
                account.ProviderSubject);

            var jwtResult = _jwtAccessTokenService.IssueAccessToken(accessRequest);

            return TokenSessionResult.Success(
                jwtResult.Token,
                jwtResult.ExpiresAtUtc,
                newRawMaterial,
                newExpiresAt);
        });
    }

    public async Task<LogoutResult> LogoutAsync(
        string refreshTokenMaterial,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenMaterial))
            return new LogoutResult(false, "TOKEN_EMPTY");

        var tokenHash = _refreshTokenMaterialService.ComputeHash(refreshTokenMaterial);

        using var dbContext = _dbContextFactory.CreateDbContext();
        var executionStrategy = dbContext.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            using var transaction = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            
            var token = await dbContext.FindRefreshTokenByHashForUpdateAsync(tokenHash, cancellationToken);
            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

            if (token == null)
                return new LogoutResult(false, "TOKEN_NOT_FOUND");

            if (!token.IsRevoked)
            {
                await dbContext.RevokeFamilyAsync(token.FamilyId, RefreshToken.RevokeReasonLogout, utcNow, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new LogoutResult(true, null);
        });
    }
}
