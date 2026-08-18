using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Dashboard.DTOs;

namespace PTKD.Application.Dashboard;

public interface IDashboardService
{
    /// <summary>
    /// <paramref name="includeRevenue"/>: người gọi có quyền FINANCE_VIEW_REVENUE ở công ty này
    /// hay không. Khi false, service KHÔNG truy vấn doanh thu (TotalRevenue = 0, RevenueByMonth
    /// rỗng) và cờ CanViewRevenue = false — số tiền không rời khỏi máy chủ.
    /// </summary>
    Task<DashboardSummaryDto> GetSummaryAsync(long companyId, bool includeRevenue, CancellationToken ct = default);
}
