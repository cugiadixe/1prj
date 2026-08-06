using System;

namespace PTKD.Domain.Entities;

public class ServiceHistory
{
    public const string ActionCreated = "CREATED";
    public const string ActionRenewed = "RENEWED";
    public const string ActionPriceOverridden = "PRICE_OVERRIDDEN";
    public const string ActionCancelled = "CANCELLED";
    public const string ActionExpired = "EXPIRED";

    public long Id { get; private set; }
    public long ServiceId { get; private set; }
    public string ActionCode { get; private set; } = null!;
    public string? BeforeData { get; private set; }
    public string? AfterData { get; private set; }
    public long ActedByUserId { get; private set; }
    public string? Reason { get; private set; }
    public Guid CorrelationId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ServiceHistory() { }

    public ServiceHistory(
        long serviceId,
        string actionCode,
        string? beforeData,
        string? afterData,
        long actedByUserId,
        string? reason,
        Guid correlationId)
    {
        if (string.IsNullOrWhiteSpace(actionCode))
            throw new ArgumentException("Action code is required.", nameof(actionCode));

        ServiceId = serviceId;
        ActionCode = actionCode;
        BeforeData = beforeData;
        AfterData = afterData;
        ActedByUserId = actedByUserId;
        Reason = reason;
        CorrelationId = correlationId;
        CreatedAt = DateTime.UtcNow;
    }
}
