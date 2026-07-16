using PTKD.Domain.Security.Authentication;

namespace PTKD.Application.Security.Authentication.Interfaces;

/// <summary>
/// Read/write access to Refresh_Tokens required by the token session lifecycle service.
/// Extends IAuthenticationDbContext so the same DbContext instance can serve both
/// account and token operations within a single SERIALIZABLE transaction.
/// </summary>
public interface ITokenSessionDbContext : IAuthenticationDbContext
{
    /// <summary>
    /// Fetches a single refresh token by its SHA-256 hash with UPDLOCK, HOLDLOCK to
    /// prevent concurrent rotation of the same row. Must be called inside a
    /// SERIALIZABLE transaction.
    /// </summary>
    Task<RefreshToken?> FindRefreshTokenByHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-revokes all non-revoked tokens belonging to the given family.
    /// Executes in the caller's ambient transaction.
    /// </summary>
    Task<int> RevokeFamilyAsync(
        Guid familyId,
        string reason,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that reuse was detected on a specific token using raw SQL, avoiding
    /// row_version conflicts with RevokeFamilyAsync which also uses ExecuteUpdateAsync.
    /// </summary>
    Task MarkReuseDetectedAsync(
        long tokenId,
        DateTime reuseDetectedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new refresh token row. Caller must call SaveChangesAsync.
    /// </summary>
    void AddRefreshToken(RefreshToken token);
}
