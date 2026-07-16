namespace PTKD.Api.Auth.Models;

/// <summary>Request body for POST /api/v2/auth/login.</summary>
public sealed record LoginRequest(
    string Username,
    string Password);

/// <summary>Response body for successful login and refresh.</summary>
public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    DateTime ExpiresAtUtc,
    LoginUserInfo User);

public sealed record LoginUserInfo(
    long UserId,
    string Username,
    string? DisplayName);
