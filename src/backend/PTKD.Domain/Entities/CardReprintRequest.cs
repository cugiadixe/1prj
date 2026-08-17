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

    public void SetSubmitted(long workflowInstanceId)
    {
        if (Status != StatusDraft)
            throw new InvalidOperationException($"Cannot submit request from status {Status}");

        Status = StatusPendingApproval;
        WorkflowInstanceId = workflowInstanceId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetApproved()
    {
        if (Status != StatusPendingApproval)
            throw new InvalidOperationException($"Cannot approve request from status {Status}");

        Status = StatusApproved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRejected()
    {
        if (Status != StatusPendingApproval)
            throw new InvalidOperationException($"Cannot reject request from status {Status}");

        Status = StatusRejected;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPaymentDraft(long paymentTransactionId, long serviceItemId, decimal feeAmount, string feeCurrency)
    {
        if (Status != StatusApproved)
            throw new InvalidOperationException($"Cannot set payment draft from status {Status}");

        Status = StatusPendingPayment;
        PaymentTransactionId = paymentTransactionId;
        ServiceItemId = serviceItemId;
        FeeAmount = feeAmount;
        FeeCurrency = feeCurrency;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPaid()
    {
        if (Status != StatusPendingPayment)
            throw new InvalidOperationException($"Cannot mark as paid from status {Status}");

        Status = StatusPaid;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrinted(long printedByUserId)
    {
        if (Status != StatusPaid)
            throw new InvalidOperationException($"Cannot mark as printed from status {Status}");

        Status = StatusPrinted;
        PrintedAt = DateTime.UtcNow;
        PrintedByUserId = printedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// In LẦN ĐẦU: bỏ qua duyệt + phí, đi thẳng DRAFT → PRINTED. Chỉ hợp lệ cho yêu cầu loại
    /// INITIAL_PRINT (tầng service kiểm lại số lần in thực tế trong giao dịch trước khi gọi).
    /// </summary>
    public void SetPrintedInitial(long printedByUserId)
    {
        if (Status != StatusDraft)
            throw new InvalidOperationException($"Cannot direct-print from status {Status}");
        if (RequestType != TypeInitialPrint)
            throw new InvalidOperationException("Direct print (no approval) is only valid for INITIAL_PRINT.");

        Status = StatusPrinted;
        PrintedAt = DateTime.UtcNow;
        PrintedByUserId = printedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetReleased(long releasedByUserId)
    {
        if (Status != StatusPrinted)
            throw new InvalidOperationException($"Cannot mark as released from status {Status}");

        Status = StatusReleased;
        ReleasedAt = DateTime.UtcNow;
        ReleasedByUserId = releasedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }
}
