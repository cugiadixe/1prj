namespace PTKD.Application.Security.Authentication.Models;

public sealed record AuthenticateAccountCommand(
    string ProviderType,
    string ProviderSubject,
    string Password);

public sealed record ChangePasswordCommand(
    long AccountId,
    string CurrentPassword,
    string NewPassword,
    byte[] TargetRowVersion,
    long ActingUserId);

public sealed record AdministratorResetPasswordCommand(
    long AccountId,
    string TemporaryPassword,
    byte[] TargetRowVersion,
    long ActingUserId);

public sealed record AdministratorUnlockAccountCommand(
    long AccountId,
    byte[] TargetRowVersion,
    long ActingUserId);

public sealed record DisableAuthenticationAccountCommand(
    long AccountId,
    byte[] TargetRowVersion,
    long ActingUserId);
