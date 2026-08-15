using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Application.Security.Authorization;

/// <summary>
/// Áp phạm vi công ty lên dữ liệu gắn với KHÁCH HÀNG.
///
/// Khách hàng không mang cột công ty trực tiếp mà nối qua bảng Customer_Company_Contexts, và một
/// khách có thể thuộc nhiều công ty. Quy tắc thống nhất: người gọi thấy/sửa được một khách hàng
/// nếu TỒN TẠI một công ty của khách đó mà quyền của họ phủ tới.
///
/// Gom vào một chỗ vì trước đây mỗi module tự nghĩ một cách lọc — đó là lý do vá được module này
/// lại sót module kia, và hai đường trong CÙNG một service cũng lệch nhau.
/// </summary>
public static class CustomerCompanyScope
{
    /// <summary>
    /// Lọc một tập id khách hàng xuống còn những khách người gọi được phép.
    /// Dùng cho danh sách đã nạp về (tập nhỏ), tránh phải dựng bộ lọc tổng quát bằng cây biểu thức.
    /// </summary>
    public static async Task<HashSet<long>> FilterAccessibleCustomerIdsAsync(
        IOrganizationDbContext context,
        IReadOnlyCollection<long> customerIds,
        PermissionScopeResult scope,
        CancellationToken ct)
    {
        if (!scope.Granted || customerIds.Count == 0)
            return new HashSet<long>();

        var links = await context.CustomerCompanyContexts
            .AsNoTracking()
            .Where(cc => customerIds.Contains(cc.CustomerId))
            .Select(cc => new { cc.CustomerId, cc.CompanyId })
            .ToListAsync(ct);

        var accessible = links
            .Where(l => scope.Allows(l.CompanyId))
            .Select(l => l.CustomerId)
            .ToHashSet();

        if (scope.IsUnrestricted)
        {
            // Khách mồ côi (chưa gắn công ty nào) chỉ người được cấp toàn cục mới thấy —
            // nếu không thì không ai chạm được vào chúng nữa.
            var linked = links.Select(l => l.CustomerId).ToHashSet();
            foreach (var id in customerIds.Where(id => !linked.Contains(id)))
                accessible.Add(id);
        }

        return accessible;
    }

    /// <summary>
    /// Người gọi có được thao tác trên khách hàng này không.
    ///
    /// Khách chưa gắn công ty nào là dữ liệu mồ côi: chỉ người được cấp TOÀN CỤC mới chạm được,
    /// nếu không thì không ai sửa được nó nữa.
    /// </summary>
    public static async Task<bool> CanAccessCustomerAsync(
        IOrganizationDbContext context,
        long customerId,
        PermissionScopeResult scope,
        CancellationToken ct)
    {
        if (!scope.Granted)
            return false;

        var companyIds = await context.CustomerCompanyContexts
            .AsNoTracking()
            .Where(cc => cc.CustomerId == customerId)
            .Select(cc => cc.CompanyId)
            .ToListAsync(ct);

        if (companyIds.Count == 0)
            return scope.IsUnrestricted;

        return companyIds.Any(scope.Allows);
    }

    /// <summary>
    /// Như <see cref="CanAccessCustomerAsync"/> nhưng ném lỗi 403 thay vì trả false.
    /// Dùng cho đường GHI, nơi im lặng bỏ qua là nguy hiểm.
    /// </summary>
    public static async Task EnsureCustomerAccessibleAsync(
        IOrganizationDbContext context,
        long customerId,
        PermissionScopeResult scope,
        string errorCode,
        CancellationToken ct)
    {
        if (!await CanAccessCustomerAsync(context, customerId, scope, ct))
            throw new PermissionDeniedException(errorCode,
                "Bản ghi này thuộc công ty bạn không có quyền thao tác.");
    }
}
