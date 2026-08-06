using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.PaymentManagement.DTOs;
using PTKD.Domain.Entities;

namespace PTKD.Application.PaymentManagement.Services;

public class ReconciliationService : IReconciliationService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public ReconciliationService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<DailyReconciliationReportDto> GetDailyReportAsync(long companyId, DateTime date, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var dailyDate = date.Date;
        var nextDay = dailyDate.AddDays(1);

        var payments = await db.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId
                && p.Status == PaymentTransaction.StatusConfirmed
                && !p.IsDeleted
                && p.PaymentDate >= dailyDate
                && p.PaymentDate < nextDay)
            .OrderBy(p => p.PaymentDate)
            .ToArrayAsync(ct);

        var period = await db.ReconciliationPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(rp => rp.CompanyId == companyId
                && rp.PeriodType == "DAILY"
                && rp.PeriodDate == dailyDate, ct);

        return new DailyReconciliationReportDto
        {
            CompanyId = companyId,
            Date = dailyDate,
            Period = period != null ? MapPeriodToDto(period) : null,
            Payments = payments.Select(p => new PaymentTransactionListDto
            {
                Id = p.Id,
                BillCode = p.BillCode,
                CompanyId = p.CompanyId,
                CustomerId = p.CustomerId,
                PaymentMethod = p.PaymentMethod,
                PaymentDate = p.PaymentDate,
                TotalAmount = p.TotalAmount,
                Status = p.Status,
                CreatedAt = p.CreatedAt
            }).ToList(),
            TotalAmount = payments.Sum(p => p.TotalAmount),
            TransactionCount = payments.Length
        };
    }

    public async Task<MonthlyReconciliationReportDto> GetMonthlyReportAsync(long companyId, int year, int month, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);
        var daysInMonth = (endDate - startDate).Days;

        var payments = await db.PaymentTransactions
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId
                && p.Status == PaymentTransaction.StatusConfirmed
                && !p.IsDeleted
                && p.PaymentDate >= startDate
                && p.PaymentDate < endDate)
            .ToArrayAsync(ct);

        var dailyPeriods = await db.ReconciliationPeriods
            .AsNoTracking()
            .Where(rp => rp.CompanyId == companyId
                && rp.PeriodType == "DAILY"
                && rp.PeriodDate >= startDate
                && rp.PeriodDate < endDate)
            .ToDictionaryAsync(rp => rp.PeriodDate.Date, ct);

        var dailySummaries = new System.Collections.Generic.List<DailySummaryDto>();
        for (int day = 0; day < daysInMonth; day++)
        {
            var dayDate = startDate.AddDays(day);
            var nextDay = dayDate.AddDays(1);
            var dayPayments = payments.Where(p => p.PaymentDate >= dayDate && p.PaymentDate < nextDay).ToArray();
            dailyPeriods.TryGetValue(dayDate, out var dayPeriod);

            if (dayPayments.Length > 0 || dayPeriod != null)
            {
                dailySummaries.Add(new DailySummaryDto
                {
                    Date = dayDate,
                    TotalAmount = dayPayments.Sum(p => p.TotalAmount),
                    TransactionCount = dayPayments.Length,
                    PeriodStatus = dayPeriod?.Status
                });
            }
        }

        return new MonthlyReconciliationReportDto
        {
            CompanyId = companyId,
            Year = year,
            Month = month,
            DailySummaries = dailySummaries,
            MonthlyTotalAmount = payments.Sum(p => p.TotalAmount),
            MonthlyTransactionCount = payments.Length
        };
    }

    public async Task<ReconciliationPeriodDto?> GetPeriodByIdAsync(long periodId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var period = await db.ReconciliationPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(rp => rp.Id == periodId, ct);

        if (period == null) return null;
        return MapPeriodToDto(period);
    }

    public async Task<ReconciliationPeriodDto> PrepareAsync(long periodId, PrepareReconciliationRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var period = await db.ReconciliationPeriods
            .FirstOrDefaultAsync(rp => rp.Id == periodId, ct)
            ?? throw new InvalidOperationException("Reconciliation period not found.");

        db.Entry(period).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);

        DateTime periodStart;
        DateTime periodEnd;

        if (period.PeriodType == "DAILY")
        {
            periodStart = period.PeriodDate.Date;
            periodEnd = periodStart.AddDays(1);
        }
        else
        {
            periodStart = period.PeriodDate.Date;
            periodEnd = periodStart.AddMonths(1);
        }

        var aggregate = await db.PaymentTransactions
            .Where(p => p.CompanyId == period.CompanyId
                && p.Status == PaymentTransaction.StatusConfirmed
                && !p.IsDeleted
                && p.PaymentDate >= periodStart
                && p.PaymentDate < periodEnd)
            .GroupBy(p => 1)
            .Select(g => new { Total = g.Sum(p => p.TotalAmount), Count = g.Count() })
            .FirstOrDefaultAsync(ct);

        period.Prepare(actorUserId, aggregate?.Total ?? 0, aggregate?.Count ?? 0);
        await db.SaveChangesAsync(ct);

        return MapPeriodToDto(period);
    }

    public async Task<ReconciliationPeriodDto> ConfirmAsync(long periodId, ConfirmReconciliationRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var period = await db.ReconciliationPeriods
            .FirstOrDefaultAsync(rp => rp.Id == periodId, ct)
            ?? throw new InvalidOperationException("Reconciliation period not found.");

        db.Entry(period).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);

        period.Confirm(actorUserId);
        await db.SaveChangesAsync(ct);

        return MapPeriodToDto(period);
    }

    private static ReconciliationPeriodDto MapPeriodToDto(ReconciliationPeriod entity)
    {
        return new ReconciliationPeriodDto
        {
            Id = entity.Id,
            CompanyId = entity.CompanyId,
            PeriodType = entity.PeriodType,
            PeriodDate = entity.PeriodDate,
            Status = entity.Status,
            TotalAmount = entity.TotalAmount,
            TransactionCount = entity.TransactionCount,
            PreparedByUserId = entity.PreparedByUserId,
            PreparedAt = entity.PreparedAt,
            ConfirmedByUserId = entity.ConfirmedByUserId,
            ConfirmedAt = entity.ConfirmedAt,
            RowVersion = Convert.ToBase64String(entity.RowVersion)
        };
    }
}
