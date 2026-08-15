using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Domain.Entities;

namespace PTKD.Application.Security.Authorization;

/// <summary>
/// Áp phạm vi công ty lên dữ liệu MỘ.
///
/// Mộ thuộc công ty QUA nghĩa trang (Grave.CemeteryId -> Cemetery.CompanyId) — mỗi mộ đúng MỘT
/// công ty, khác với khách hàng (nhiều công ty). Người gọi thao tác được một mộ nếu quyền của họ
/// phủ tới công ty của nghĩa trang chứa mộ đó.
///
/// Gom vào một chỗ để mọi đường (đọc/ghi/đính kèm) lọc GIỐNG nhau — tránh vá đường này sót đường
/// kia như mô hình cũ.
/// </summary>
public static class GraveCompanyScope
{
    /// <summary>
    /// Gắn mệnh đề lọc công ty vào truy vấn MỘ, dùng cho danh sách phân trang trong CSDL (không
    /// nạp hết về rồi lọc). Không cấp quyền -> rỗng; toàn cục -> chỉ trừ công ty bị cấm; theo công
    /// ty -> chỉ các công ty được phủ.
    /// </summary>
    public static IQueryable<Grave> ApplyScope(IQueryable<Grave> query, PermissionScopeResult scope)
    {
        if (!scope.Granted)
            return query.Where(_ => false);

        if (scope.IsUnrestricted)
        {
            var excluded = scope.ExcludedCompanyIds;
            return excluded.Count == 0
                ? query
                : query.Where(g => !excluded.Contains(g.Cemetery!.CompanyId));
        }

        var allowed = scope.AllowedCompanyIds; // đã loại công ty bị cấm
        return query.Where(g => allowed.Contains(g.Cemetery!.CompanyId));
    }

    /// <summary>Người gọi có được thao tác trên mộ này không (theo công ty của nghĩa trang).</summary>
    public static async Task<bool> CanAccessGraveAsync(
        IOrganizationDbContext context,
        long graveId,
        PermissionScopeResult scope,
        CancellationToken ct)
    {
        if (!scope.Granted)
            return false;

        var companyId = await context.Graves
            .AsNoTracking()
            .Where(g => g.Id == graveId)
            .Select(g => (long?)g.Cemetery!.CompanyId)
            .FirstOrDefaultAsync(ct);

        // Mộ không tồn tại -> không có gì để cho phép (nơi gọi tự phân biệt 404 nếu cần).
        return companyId.HasValue && scope.Allows(companyId.Value);
    }

    /// <summary>
    /// Kiểm được truy cập cho một CÔNG TY đã biết (vd công ty của nghĩa trang khi TẠO mộ, lúc chưa
    /// có bản ghi mộ để tra).
    /// </summary>
    public static bool AllowsCompany(PermissionScopeResult scope, long companyId)
        => scope.Granted && scope.Allows(companyId);

    /// <summary>Như <see cref="CanAccessGraveAsync"/> nhưng ném 403 — dùng cho đường GHI.</summary>
    public static async Task EnsureGraveAccessibleAsync(
        IOrganizationDbContext context,
        long graveId,
        PermissionScopeResult scope,
        string errorCode,
        CancellationToken ct)
    {
        if (!await CanAccessGraveAsync(context, graveId, scope, ct))
            throw new PermissionDeniedException(errorCode,
                "Mộ này thuộc công ty bạn không có quyền thao tác.");
    }
}
