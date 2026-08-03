using System;
using System.Collections.Generic;

namespace PTKD.Application.PaymentManagement.DTOs;

public class ReconciliationPeriodDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string PeriodType { get; set; } = null!;
    public DateTime PeriodDate { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public long? PreparedByUserId { get; set; }
    public DateTime? PreparedAt { get; set; }
    public long? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class DailyReconciliationReportDto
{
    public long CompanyId { get; set; }
    public DateTime Date { get; set; }
    public ReconciliationPeriodDto? Period { get; set; }
    public List<PaymentTransactionListDto> Payments { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}

public class MonthlyReconciliationReportDto
{
    public long CompanyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public List<DailySummaryDto> DailySummaries { get; set; } = new();
    public decimal MonthlyTotalAmount { get; set; }
    public int MonthlyTransactionCount { get; set; }
}

public class DailySummaryDto
{
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public string? PeriodStatus { get; set; }
}

public class PrepareReconciliationRequest
{
    public string RowVersion { get; set; } = null!;
}

public class ConfirmReconciliationRequest
{
    public string RowVersion { get; set; } = null!;
}
