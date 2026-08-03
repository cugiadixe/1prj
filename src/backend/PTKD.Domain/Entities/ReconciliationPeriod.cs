using System;

namespace PTKD.Domain.Entities;

public class ReconciliationPeriod
{
    public const string StatusOpen = "OPEN";
    public const string StatusDirty = "DIRTY";
    public const string StatusPrepared = "PREPARED";
    public const string StatusConfirmed = "CONFIRMED";

    public long Id { get; private set; }
    public long CompanyId { get; private set; }
    public string PeriodType { get; private set; } = null!;
    public DateTime PeriodDate { get; private set; }
    public string Status { get; private set; } = null!;
    public decimal TotalAmount { get; private set; }
    public int TransactionCount { get; private set; }
    public long? PreparedByUserId { get; private set; }
    public DateTime? PreparedAt { get; private set; }
    public long? ConfirmedByUserId { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private ReconciliationPeriod() { }

    public static ReconciliationPeriod Create(long companyId, string periodType, DateTime periodDate)
    {
        if (periodType != "DAILY" && periodType != "MONTHLY")
            throw new ArgumentException("Period type must be DAILY or MONTHLY.", nameof(periodType));

        return new ReconciliationPeriod
        {
            CompanyId = companyId,
            PeriodType = periodType,
            PeriodDate = periodDate,
            Status = StatusOpen,
            TotalAmount = 0,
            TransactionCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkDirty()
    {
        if (Status == StatusConfirmed)
            throw new InvalidOperationException("Cannot mark a confirmed reconciliation period as dirty.");

        Status = StatusDirty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Prepare(long userId, decimal totalAmount, int transactionCount)
    {
        if (Status != StatusOpen && Status != StatusDirty)
            throw new InvalidOperationException($"Cannot prepare a reconciliation period in {Status} status.");

        Status = StatusPrepared;
        TotalAmount = totalAmount;
        TransactionCount = transactionCount;
        PreparedByUserId = userId;
        PreparedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm(long userId)
    {
        if (Status != StatusPrepared)
            throw new InvalidOperationException($"Cannot confirm a reconciliation period in {Status} status.");

        Status = StatusConfirmed;
        ConfirmedByUserId = userId;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAggregates(decimal totalAmount, int transactionCount)
    {
        TotalAmount = totalAmount;
        TransactionCount = transactionCount;
        UpdatedAt = DateTime.UtcNow;
    }
}
