using PTKD.Application.Security.AccountManagement.DTOs;

namespace PTKD.Application.Security.AccountManagement;

public interface IAccountManagementService
{
    // Returns null when account is not found.
    Task<AccountDetailDto?> GetAccountDetailAsync(
        long accountId,
        CancellationToken cancellationToken = default);

    Task<AccountManagementResult> ActivateAccountAsync(
        long accountId,
        long actingUserId,
        CancellationToken cancellationToken = default);

    // Reason is required per DEC-1B-I-07.
    Task<AccountManagementResult> DisableAccountAsync(
        long accountId,
        string reason,
        long actingUserId,
        CancellationToken cancellationToken = default);

    // Reason is required per DEC-1B-I-07.
    Task<AccountManagementResult> LockAccountAsync(
        long accountId,
        string reason,
        long actingUserId,
        CancellationToken cancellationToken = default);

    Task<AccountManagementResult> UnlockAccountAsync(
        long accountId,
        long actingUserId,
        CancellationToken cancellationToken = default);

    // Generates a server-side temporary password. Reason is required per DEC-1B-I-07.
    // TemporaryPassword in the result must be returned to the caller exactly once and never logged.
    Task<AccountManagementResult> AdminResetPasswordAsync(
        long accountId,
        string reason,
        long actingUserId,
        CancellationToken cancellationToken = default);

    // Reason is required per DEC-1B-I-07.
    Task<AccountManagementResult> RevokeAllSessionsAsync(
        long accountId,
        string reason,
        long actingUserId,
        CancellationToken cancellationToken = default);
}
