using System;

namespace PTKD.Domain.Entities;

public class CardReprintRequest
{
    public const string TypeInitialPrint = "INITIAL_PRINT";
    public const string TypeReprint = "REPRINT";

    public const string StatusDraft = "DRAFT";
    public const string StatusPendingApproval = "PENDING_APPROVAL";
    public const string StatusApproved = "APPROVED";
    public const string StatusRejected = "REJECTED";
    public const string StatusPendingPayment = "PENDING_PAYMENT";
    public const string StatusPaid = "PAID";
    public const string StatusPrinted = "PRINTED";
    public const string StatusReleased = "RELEASED";

    public long Id { get; private set; }
    public long CompanyId { get; private set; }
    public long CardId { get; private set; }
    public long RequesterId { get; private set; }
    public string RequestType { get; private set; } = null!;
    public int ReprintNumber { get; private set; }
    public decimal? FeeAmount { get; private set; }
    public string? FeeCurrency { get; private set; }
    public string? ReasonCode { get; private set; }
    public long? WorkflowInstanceId { get; private set; }
    public long? PaymentTransactionId { get; private set; }
    public long? ServiceItemId { get; private set; }
    public string Status { get; private set; } = null!;
    public string? Notes { get; private set; }
    public DateTime? PrintedAt { get; private set; }
    public long? PrintedByUserId { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public long? ReleasedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private CardReprintRequest() { }

    public static CardReprintRequest CreateDraft(
        long companyId, 
        long cardId, 
        long requesterId, 
        string requestType, 
        int reprintNumber, 
        string? reasonCode, 
        string? notes, 
        long createdByUserId)
    {
        return new CardReprintRequest
        {
            CompanyId = companyId,
            CardId = cardId,
            RequesterId = requesterId,
            RequestType = requestType,
            ReprintNumber = reprintNumber,
            ReasonCode = reasonCode,
            Notes = notes,
            Status = StatusDraft,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
