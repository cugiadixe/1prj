namespace PTKD.Domain.Security.Authentication;

/// <summary>
/// Domain entity representing a single opaque refresh token, mapped to dbo.Refresh_Tokens (V0003).
/// Only the SHA-256 hash of the raw token material is stored. Raw token material must never be
/// persisted or logged anywhere in the application.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
        // EF Core materialisation only.
    }

    /// <summary>
    /// Creates a new refresh token row for initial login (family root).
    /// </summary>
    public static RefreshToken CreateRoot(
        long accountId,
        string tokenHash,
        Guid familyId,
        Guid sessionId,
        DateTime issuedAt,
        DateTime expiresAt,
        string? ipAddress,
        string? userAgent)
    {
        EnsureUtc(issuedAt);
        EnsureUtc(expiresAt);
        if (expiresAt <= issuedAt)
            throw new ArgumentException("expiresAt must be after issuedAt.", nameof(expiresAt));
        EnsureHashLength(tokenHash);
        if (accountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(accountId));
        if (familyId == Guid.Empty)
            throw new ArgumentException("familyId must not be empty.", nameof(familyId));
        if (sessionId == Guid.Empty)
            throw new ArgumentException("sessionId must not be empty.", nameof(sessionId));

        return new RefreshToken
        {
            AccountId = accountId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            SessionId = sessionId,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            CreatedIpAddress = ipAddress,
            CreatedUserAgent = userAgent
        };
    }

    /// <summary>
    /// Creates the replacement token row produced during strict single-use rotation.
    /// </summary>
    public static RefreshToken CreateReplacement(
        long accountId,
        string tokenHash,
        Guid familyId,
        Guid sessionId,
        DateTime issuedAt,
        DateTime expiresAt,
        long replacedById,
        string? ipAddress,
        string? userAgent)
    {
        EnsureUtc(issuedAt);
        EnsureUtc(expiresAt);
        if (expiresAt <= issuedAt)
            throw new ArgumentException("expiresAt must be after issuedAt.", nameof(expiresAt));
        EnsureHashLength(tokenHash);
        if (accountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(accountId));
        if (familyId == Guid.Empty)
            throw new ArgumentException("familyId must not be empty.", nameof(familyId));
        if (sessionId == Guid.Empty)
            throw new ArgumentException("sessionId must not be empty.", nameof(sessionId));
        if (replacedById <= 0)
            throw new ArgumentOutOfRangeException(nameof(replacedById));

        // replacedById refers to the predecessor token that is being replaced.
        // The constraint FK_RefreshTokens_ReplacedBy is set by the predecessor row's replaced_by_token_id column.
        return new RefreshToken
        {
            AccountId = accountId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            SessionId = sessionId,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            CreatedIpAddress = ipAddress,
            CreatedUserAgent = userAgent
        };
    }

    // ─── Persisted columns ───────────────────────────────────────────────────

    public long Id { get; private set; }
    public long AccountId { get; private set; }

    /// <summary>
    /// SHA-256 hex hash of the opaque token material. Raw material is never stored.
    /// Fixed char(64) — uppercase hex.
    /// </summary>
    public string TokenHash { get; private set; } = null!;

    public Guid FamilyId { get; private set; }
    public Guid SessionId { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Set exactly once when this token is consumed during rotation.</summary>
    public DateTime? UsedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }
    public string? RevokeReason { get; private set; }

    /// <summary>
    /// The id of the successor token row issued when this token was rotated.
    /// The V0003 FK_RefreshTokens_ReplacedBy constraint enforces referential integrity.
    /// </summary>
    public long? ReplacedByTokenId { get; private set; }

    /// <summary>
    /// Set when a reuse of an already-consumed token is detected for this row.
    /// </summary>
    public DateTime? ReuseDetectedAt { get; private set; }

    public string? CreatedIpAddress { get; private set; }
    public string? CreatedUserAgent { get; private set; }

    /// <summary>rowversion column — used for optimistic concurrency detection.</summary>
    public byte[] RowVersion { get; private set; } = null!;

    // ─── Domain behaviour ────────────────────────────────────────────────────

    /// <summary>Returns true if the token has passed its expiry time.</summary>
    public bool IsExpired(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        return utcNow >= ExpiresAt;
    }

    /// <summary>Returns true if the token has already been used (consumed by rotation).</summary>
    public bool IsUsed => UsedAt.HasValue;

    /// <summary>Returns true if the token or its family has been explicitly revoked.</summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>Returns true if this token is valid and may be exchanged during rotation.</summary>
    public bool IsUsable(DateTime utcNow) => !IsUsed && !IsRevoked && !IsExpired(utcNow);

    /// <summary>
    /// Mark this token as consumed and point to its replacement.
    /// Must be called in a SERIALIZABLE transaction with UPDLOCK/HOLDLOCK on this row.
    /// </summary>
    public void MarkUsed(long replacementTokenId, DateTime utcNow)
    {
        EnsureUtc(utcNow);
        if (IsUsed)
            throw new InvalidOperationException("Token is already marked as used.");
        if (IsRevoked)
            throw new InvalidOperationException("Cannot mark a revoked token as used.");
        if (replacementTokenId <= 0)
            throw new ArgumentOutOfRangeException(nameof(replacementTokenId));

        UsedAt = utcNow;
        ReplacedByTokenId = replacementTokenId;
    }

    /// <summary>
    /// Revoke this token individually with a reason.
    /// </summary>
    public void Revoke(string reason, DateTime utcNow)
    {
        EnsureUtc(utcNow);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Revocation reason is required.", nameof(reason));
        if (IsRevoked)
            return; // Idempotent — already revoked.

        RevokedAt = utcNow;
        RevokeReason = reason;
    }

    /// <summary>
    /// Record that this token was presented again after already being consumed —
    /// a theft or replay indicator. Does not revoke; callers must revoke the family separately.
    /// </summary>
    public void RecordReuseDetected(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        if (!ReuseDetectedAt.HasValue)
            ReuseDetectedAt = utcNow;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be UTC.", nameof(value));
    }

    private static void EnsureHashLength(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash) || hash.Length != 64)
            throw new ArgumentException("Token hash must be a 64-character hex string (SHA-256).", nameof(hash));
    }

    public const string RevokeReasonReuseDetected = "REUSE_DETECTED";
    public const string RevokeReasonLogout = "LOGOUT";
}
