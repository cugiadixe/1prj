using PTKD.Application.Common.Models;
using PTKD.Application.Security.AccountManagement.DTOs;

namespace PTKD.Application.Security.AccountManagement;

public sealed record UserWithoutAccountDto(long UserId, string FullName, string? EmployeeCode, string? Email);

public interface IAccountManagementService
{
    // K0 discovery: list/search accounts. Page and PageSize validated by service (max 100).
    Task<PagedResult<AccountSummaryDto>> SearchAccountsAsync(
        AccountSearchParameters parameters,
        CancellationToken cancellationToken = default);

    // K0 discovery: by-user lookup. Returns USER_NOT_FOUND when userId does not exist.
    // Returns empty list when user exists but has no auth accounts.
    Task<(IReadOnlyList<AccountSummaryDto> Accounts, bool UserExists)> GetAccountsByUserIdAsync(
        long userId,
        CancellationToken cancellationToken = default);

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

    Task<AccountManagementResult> CreateInternalAccountAsync(
        long userId,
        string providerSubject,
        long actingUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserWithoutAccountDto>> GetUsersWithoutAccountAsync(
        CancellationToken cancellationToken = default);
}
