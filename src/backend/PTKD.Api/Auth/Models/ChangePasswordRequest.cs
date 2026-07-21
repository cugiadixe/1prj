namespace PTKD.Api.Auth.Models;

/// <summary>Request body for POST /api/v2/auth/change-password.</summary>
public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
