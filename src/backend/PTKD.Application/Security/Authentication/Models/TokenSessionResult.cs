using System;

namespace PTKD.Application.Security.Authentication.Models;

public enum TokenSessionStatus
{
    Success,
    InvalidCredentials,
    RefreshTokenInvalid,
    RefreshTokenReused,
    SessionRevoked,
    AccountDisabled,
    AccountLocked
}

public sealed record TokenSessionResult
{
    private TokenSessionResult(
        TokenSessionStatus status,
        string? internalReason,
        string? accessToken,
        DateTime? accessTokenExpiresAtUtc,
        string? refreshTokenMaterial,
        DateTime? refreshTokenExpiresAtUtc,
        bool mustChangePassword)
    {
        Status = status;
        InternalReason = internalReason;
        AccessToken = accessToken;
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        RefreshTokenMaterial = refreshTokenMaterial;
        RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
        MustChangePassword = mustChangePassword;
    }

    public TokenSessionStatus Status { get; }
    
    /// <summary>
    /// Internal reason code for tests and audit. Must not be exposed to clients directly.
    /// </summary>
    public string? InternalReason { get; }
    
    public string? AccessToken { get; }
    public DateTime? AccessTokenExpiresAtUtc { get; }
    public string? RefreshTokenMaterial { get; }
    public DateTime? RefreshTokenExpiresAtUtc { get; }
    public bool MustChangePassword { get; }
    
    public bool IsSuccess => Status == TokenSessionStatus.Success;

    public static TokenSessionResult Success(
        string accessToken,
        DateTime accessTokenExpiresAtUtc,
        string refreshTokenMaterial,
        DateTime refreshTokenExpiresAtUtc,
        bool mustChangePassword)
        => new TokenSessionResult(
            TokenSessionStatus.Success,
            null,
            accessToken,
            accessTokenExpiresAtUtc,
            refreshTokenMaterial,
            refreshTokenExpiresAtUtc,
            mustChangePassword);

    public static TokenSessionResult Failure(TokenSessionStatus status, string internalReason)
        => new TokenSessionResult(status, internalReason, null, null, null, null, false);
}

public sealed record LogoutResult(bool IsSuccess, string? InternalReason);
