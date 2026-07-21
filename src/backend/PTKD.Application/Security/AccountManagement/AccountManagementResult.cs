namespace PTKD.Application.Security.AccountManagement;

public sealed record AccountManagementResult
{
    public bool Succeeded { get; init; }
    public string? ErrorCode { get; init; }

    // Only populated by AdminResetPasswordAsync. Cleared from memory once the caller reads it.
    // Never log this value (SEC-005, DEC-1B-I-03).
    public string? TemporaryPassword { get; init; }

    public static AccountManagementResult Success() =>
        new() { Succeeded = true };

    public static AccountManagementResult SuccessWithPassword(string temporaryPassword) =>
        new() { Succeeded = true, TemporaryPassword = temporaryPassword };

    public static AccountManagementResult Failure(string errorCode) =>
        new() { Succeeded = false, ErrorCode = errorCode };
}
