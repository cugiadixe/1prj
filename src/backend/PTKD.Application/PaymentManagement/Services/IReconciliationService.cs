using System;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.PaymentManagement.DTOs;

namespace PTKD.Application.PaymentManagement.Services;

public interface IReconciliationService
{
    Task<DailyReconciliationReportDto> GetDailyReportAsync(long companyId, DateTime date, CancellationToken ct = default);
    Task<MonthlyReconciliationReportDto> GetMonthlyReportAsync(long companyId, int year, int month, CancellationToken ct = default);
    Task<ReconciliationPeriodDto?> GetPeriodByIdAsync(long periodId, CancellationToken ct = default);
    Task<ReconciliationPeriodDto> PrepareAsync(long periodId, PrepareReconciliationRequest request, long actorUserId, CancellationToken ct = default);
    Task<ReconciliationPeriodDto> ConfirmAsync(long periodId, ConfirmReconciliationRequest request, long actorUserId, CancellationToken ct = default);
}
