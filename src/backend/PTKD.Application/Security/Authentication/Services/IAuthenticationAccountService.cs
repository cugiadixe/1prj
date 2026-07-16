using PTKD.Application.Security.Authentication.Models;

namespace PTKD.Application.Security.Authentication.Services;

public interface IAuthenticationAccountService
{
    Task<AuthenticationAttemptResult> AuthenticateAsync(
        AuthenticateAccountCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthenticationAccountOperationResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthenticationAccountOperationResult> AdministratorResetPasswordAsync(
        AdministratorResetPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthenticationAccountOperationResult> AdministratorUnlockAsync(
        AdministratorUnlockAccountCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthenticationAccountOperationResult> DisableAccountAsync(
        DisableAuthenticationAccountCommand command,
        CancellationToken cancellationToken = default);
}
