namespace PTKD.Application.Security.Authentication.Interfaces;

/// <summary>
/// Generates and validates JWT access tokens.
/// </summary>
public interface IJwtAccessTokenService
{
    /// <summary>
    /// Issues a signed JWT access token for a successfully-authenticated session.
    /// Claims included: sub, auth_account_id, sid, fid, jti, security_stamp, iat, nbf, exp.
    /// Claims NOT included: permissions, roles, company scope.
    /// </summary>
    JwtIssuanceResult IssueAccessToken(AccessTokenRequest request);

    /// <summary>
    /// Validates a JWT access token and extracts the embedded claims needed for
    /// server-side account/session state verification.
    /// Returns null if the token is cryptographically invalid, expired beyond clock skew,
    /// or missing required claims. Callers must additionally verify account state against DB.
    /// </summary>
    JwtValidationResult? ValidateAccessToken(string jwt);
}

/// <summary>
/// Input for JWT access token issuance.
/// </summary>
public sealed record AccessTokenRequest(
    long UserId,
    long AccountId,
    Guid SessionId,
    Guid FamilyId,
    Guid SecurityStamp,
    string Username,
    bool MustChangePassword);

/// <summary>
/// Result of JWT access token issuance.
/// </summary>
public sealed record JwtIssuanceResult(
    string Token,
    DateTime ExpiresAtUtc);

/// <summary>
/// Validated claims extracted from a JWT access token.
/// Does not represent an authorization decision — callers must verify account state against DB.
/// </summary>
public sealed record JwtValidationResult(
    long UserId,
    long AccountId,
    Guid SessionId,
    Guid FamilyId,
    Guid SecurityStamp,
    string Username,
    string TokenId);
