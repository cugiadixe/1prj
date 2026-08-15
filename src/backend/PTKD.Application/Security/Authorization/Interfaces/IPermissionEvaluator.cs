using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Application.Security.Authorization.Interfaces;

public interface IPermissionEvaluator
{
    /// <summary>
    /// Đường chính: trả lời "có quyền không, VÀ ở những công ty nào". Endpoint cần lọc dữ liệu
    /// theo công ty thì dùng hàm này chứ không dùng <see cref="EvaluateAsync"/>.
    /// </summary>
    Task<PermissionScopeResult> ResolveAsync(
        long userId,
        string permissionCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lớp bọc có/không. <paramref name="companyId"/> null đòi phải có lần cấp TOÀN CỤC.
    /// </summary>
    Task<bool> EvaluateAsync(
        long userId,
        string permissionCode,
        long? companyId,
        CancellationToken cancellationToken = default);

    /// <summary>Bộ quyền hiệu dụng kèm phạm vi thật của từng mã.</summary>
    Task<IReadOnlyList<EffectivePermissionEntry>> GetEffectivePermissionEntriesAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        long userId,
        long? companyId,
        CancellationToken cancellationToken = default);
}
