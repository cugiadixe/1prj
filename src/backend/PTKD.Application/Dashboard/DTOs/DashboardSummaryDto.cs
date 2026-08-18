using System.Collections.Generic;

namespace PTKD.Application.Dashboard.DTOs;

public class DashboardCountItem
{
    public string Key { get; set; } = string.Empty;
    public long Count { get; set; }
}

public class DashboardMonthAmount
{
    public string Month { get; set; } = string.Empty; // "yyyy-MM"
    public decimal Amount { get; set; }
}

public class DashboardMonthCount
{
    public string Month { get; set; } = string.Empty; // "yyyy-MM"
    public long Count { get; set; }
}

public class DashboardSummaryDto
{
    // Người gọi có quyền xem doanh thu (FINANCE_VIEW_REVENUE) ở công ty này không. Khi false,
    // TotalRevenue = 0 và RevenueByMonth rỗng — frontend thay hai widget doanh thu bằng widget khác.
    public bool CanViewRevenue { get; set; }

    // KPI
    public long TotalCustomers { get; set; }
    public long TotalGraves { get; set; }
    public long OccupiedGraves { get; set; }
    public decimal TotalRevenue { get; set; }
    public long ActiveCarePackages { get; set; }

    // Charts
    public List<DashboardCountItem> GravesByStatus { get; set; } = new();
    public List<DashboardCountItem> GravesByZone { get; set; } = new();
    public List<DashboardCountItem> GravesByType { get; set; } = new();
    public List<DashboardCountItem> CustomersByStatus { get; set; } = new();
    public List<DashboardCountItem> CarePackagesByStatus { get; set; } = new();
    public List<DashboardCountItem> ServicesByStatus { get; set; } = new();
    public List<DashboardMonthAmount> RevenueByMonth { get; set; } = new();
    public List<DashboardMonthCount> CarePackagesByMonth { get; set; } = new();
}
