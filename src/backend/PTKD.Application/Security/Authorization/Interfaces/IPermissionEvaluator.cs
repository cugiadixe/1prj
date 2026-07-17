namespace PTKD.Application.Security.Authorization.Interfaces;

public interface IPermissionEvaluator
{
    Task<bool> EvaluateAsync(
        long userId,
        string permissionCode,
        long? companyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        long userId,
        long? companyId,
        CancellationToken cancellationToken = default);
}
