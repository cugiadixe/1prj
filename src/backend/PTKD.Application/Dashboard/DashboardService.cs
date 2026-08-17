using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Dashboard.DTOs;
using PTKD.Domain.Entities;

namespace PTKD.Application.Dashboard;

public class DashboardService : IDashboardService
{
    private const int MonthsWindow = 6;

    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public DashboardService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(long companyId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        // Mộ thuộc công ty QUA nghĩa trang.
        var gravesQuery = db.Graves.AsNoTracking()
            .Where(g => db.Cemeteries.Any(cm => cm.Id == g.CemeteryId && cm.CompanyId == companyId));

        var gravesByStatus = await GroupCountAsync(gravesQuery, g => g.Status, ct);
        var gravesByZone = await GroupCountAsync(gravesQuery, g => g.Zone, ct);
        var gravesByType = await GroupCountAsync(gravesQuery, g => g.GraveType, ct);

        // Khách hàng thuộc công ty QUA bối cảnh công ty.
        var customersQuery = db.Customers.AsNoTracking()
            .Where(cust => db.CustomerCompanyContexts.Any(cc => cc.CustomerId == cust.Id && cc.CompanyId == companyId));
        var customersByStatus = await GroupCountAsync(customersQuery, c => c.CustomerStatus, ct);

        var carePackagesQuery = db.CarePackageRequests.AsNoTracking().Where(r => r.CompanyId == companyId);
        var carePackagesByStatus = await GroupCountAsync(carePackagesQuery, r => r.Status, ct);

        var servicesByStatus = await GroupCountAsync(
            db.Services.AsNoTracking().Where(s => s.CompanyId == companyId), s => s.Status, ct);

        // Cửa sổ 6 tháng gần nhất (đầu tháng thứ 5 trước → nay).
        var now = DateTime.UtcNow;
        var windowStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(MonthsWindow - 1));

        var revenueRaw = await db.PaymentTransactions.AsNoTracking()
            .Where(p => p.CompanyId == companyId && !p.IsDeleted && p.PaymentDate >= windowStart)
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(x => new { x.Key.Year, x.Key.Month, Amount = x.Sum(p => p.TotalAmount) })
            .ToListAsync(ct);

        var carePkgRaw = await carePackagesQuery
            .Where(r => r.SaleDate >= windowStart)
            .GroupBy(r => new { r.SaleDate.Year, r.SaleDate.Month })
            .Select(x => new { x.Key.Year, x.Key.Month, Count = x.LongCount() })
            .ToListAsync(ct);

        var months = BuildMonthWindow(windowStart);
        var revenueByMonth = months.Select(m => new DashboardMonthAmount
        {
            Month = m.Label,
            Amount = revenueRaw.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Amount ?? 0m,
        }).ToList();
        var carePackagesByMonth = months.Select(m => new DashboardMonthCount
        {
            Month = m.Label,
            Count = carePkgRaw.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month)?.Count ?? 0L,
        }).ToList();

        // KPI
        var totalRevenue = await db.PaymentTransactions.AsNoTracking()
            .Where(p => p.CompanyId == companyId && !p.IsDeleted)
            .SumAsync(p => (decimal?)p.TotalAmount, ct) ?? 0m;
        var activeCarePackages = carePackagesByStatus
            .Where(x => x.Key == CarePackageRequest.StatusActive).Sum(x => x.Count);

        return new DashboardSummaryDto
        {
            TotalCustomers = customersByStatus.Sum(x => x.Count),
            TotalGraves = gravesByStatus.Sum(x => x.Count),
            OccupiedGraves = gravesByStatus.Where(x => x.Key == Grave.StatusOccupied).Sum(x => x.Count),
            TotalRevenue = totalRevenue,
            ActiveCarePackages = activeCarePackages,
            GravesByStatus = gravesByStatus,
            GravesByZone = gravesByZone,
            GravesByType = gravesByType,
            CustomersByStatus = customersByStatus,
            CarePackagesByStatus = carePackagesByStatus,
            ServicesByStatus = servicesByStatus,
            RevenueByMonth = revenueByMonth,
            CarePackagesByMonth = carePackagesByMonth,
        };
    }

    private static async Task<List<DashboardCountItem>> GroupCountAsync<T>(
        IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, string>> keySelector,
        CancellationToken ct)
    {
        var raw = await query
            .GroupBy(keySelector)
            .Select(g => new DashboardCountItem { Key = g.Key, Count = g.LongCount() })
            .ToListAsync(ct);
        return raw.OrderByDescending(x => x.Count).ToList();
    }

    private static List<(int Year, int Month, string Label)> BuildMonthWindow(DateTime start)
    {
        var list = new List<(int, int, string)>();
        for (var i = 0; i < MonthsWindow; i++)
        {
            var m = start.AddMonths(i);
            list.Add((m.Year, m.Month, m.ToString("yyyy-MM", CultureInfo.InvariantCulture)));
        }
        return list;
    }
}
