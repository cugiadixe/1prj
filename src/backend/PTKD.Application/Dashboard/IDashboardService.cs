using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Dashboard.DTOs;

namespace PTKD.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(long companyId, CancellationToken ct = default);
}
