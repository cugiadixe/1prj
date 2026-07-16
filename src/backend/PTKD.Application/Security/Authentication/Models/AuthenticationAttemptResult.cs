namespace PTKD.Application.Security.Authentication.Models;

public static class AuthenticationErrorCodes
{
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string PasswordChangeRequired = "AUTH_PASSWORD_CHANGE_REQUIRED";
    public const string PasswordLengthInvalid = "AUTH_PASSWORD_LENGTH_INVALID";
    public const string PasswordContainsProviderSubject = "AUTH_PASSWORD_CONTAINS_PROVIDER_SUBJECT";
    public const string PasswordReuse = "AUTH_PASSWORD_REUSE";
    public const string AccountNotFound = "AUTH_ACCOUNT_NOT_FOUND";
    public const string AccountConcurrencyConflict = "AUTH_ACCOUNT_CONCURRENCY_CONFLICT";
    public const string AccountStateConflict = "AUTH_ACCOUNT_STATE_CONFLICT";
    public const string ExternalPasswordManaged = "AUTH_EXTERNAL_PASSWORD_MANAGED";
    public const string UnexpectedDatabaseError = "AUTH_UNEXPECTED_DATABASE_ERROR";
    public const string AccountLocked = "AUTH_ACCOUNT_LOCKED";
}

public enum AuthenticationAttemptOutcome
{
    InvalidCredentials = 0,
    Succeeded = 1,
    PasswordChangeRequired = 2,
    InfrastructureFailure = 3,
    AccountLocked = 4
}

public sealed record AuthenticationAttemptResult
{
    private AuthenticationAttemptResult(
        AuthenticationAttemptOutcome outcome,
        string? errorCode,
        long? accountId,
        long? userId,
        Guid? securityStamp,
        byte[]? rowVersion)
    {
        Outcome = outcome;
        ErrorCode = errorCode;
        AccountId = accountId;
        UserId = userId;
        SecurityStamp = securityStamp;
        RowVersion = rowVersion?.ToArray();
    }

    public AuthenticationAttemptOutcome Outcome { get; }
    public string? ErrorCode { get; }
    public long? AccountId { get; }
    public long? UserId { get; }
    public Guid? SecurityStamp { get; }
    public byte[]? RowVersion { get; }
    public bool IsSuccess => Outcome is AuthenticationAttemptOutcome.Succeeded
        or AuthenticationAttemptOutcome.PasswordChangeRequired;

    public static AuthenticationAttemptResult InvalidCredentials() =>
        new(AuthenticationAttemptOutcome.InvalidCredentials, AuthenticationErrorCodes.InvalidCredentials, null, null, null, null);

    public static AuthenticationAttemptResult InfrastructureFailure() =>
        new(AuthenticationAttemptOutcome.InfrastructureFailure, AuthenticationErrorCodes.UnexpectedDatabaseError, null, null, null, null);

    public static AuthenticationAttemptResult AccountLocked() =>
        new(AuthenticationAttemptOutcome.AccountLocked, AuthenticationErrorCodes.AccountLocked, null, null, null, null);

    public static AuthenticationAttemptResult Success(
        long accountId,
        long userId,
        Guid securityStamp,
        byte[] rowVersion,
        bool passwordChangeRequired) =>
        new(
            passwordChangeRequired
                ? AuthenticationAttemptOutcome.PasswordChangeRequired
                : AuthenticationAttemptOutcome.Succeeded,
            passwordChangeRequired ? AuthenticationErrorCodes.PasswordChangeRequired : null,
            accountId,
            userId,
            securityStamp,
            rowVersion);
}

public sealed record AuthenticationAccountOperationResult
{
    private AuthenticationAccountOperationResult(bool succeeded, string? errorCode, byte[]? rowVersion)
    {
        Succeeded = succeeded;
        ErrorCode = errorCode;
        RowVersion = rowVersion?.ToArray();
    }

    public bool Succeeded { get; }
    public string? ErrorCode { get; }
    public byte[]? RowVersion { get; }

    public static AuthenticationAccountOperationResult Success(byte[] rowVersion) => new(true, null, rowVersion);
    public static AuthenticationAccountOperationResult Failure(string errorCode) => new(false, errorCode, null);
}
