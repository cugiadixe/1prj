using System;

namespace PTKD.Domain.Entities;

public class PaymentCorrectionHistory
{
    public long Id { get; private set; }
    public long PaymentTransactionId { get; private set; }
    public long CorrectedByUserId { get; private set; }
    public string Reason { get; private set; } = null!;
    public string BeforeData { get; private set; } = null!;
    public string AfterData { get; private set; } = null!;
    public string CorrectedFields { get; private set; } = null!;
    public Guid CorrelationId { get; private set; }
    public string? AffectedReconciliationPeriods { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PaymentCorrectionHistory() { }

    public PaymentCorrectionHistory(
        long paymentTransactionId,
        long correctedByUserId,
        string reason,
        string beforeData,
        string afterData,
        string correctedFields,
        Guid correlationId,
        string? affectedReconciliationPeriods)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Correction reason is required.", nameof(reason));

        PaymentTransactionId = paymentTransactionId;
        CorrectedByUserId = correctedByUserId;
        Reason = reason;
        BeforeData = beforeData;
        AfterData = afterData;
        CorrectedFields = correctedFields;
        CorrelationId = correlationId;
        AffectedReconciliationPeriods = affectedReconciliationPeriods;
        CreatedAt = DateTime.UtcNow;
    }
}
