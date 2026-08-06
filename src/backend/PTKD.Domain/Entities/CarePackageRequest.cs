using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class CarePackageRequest
{
    public const string StatusDraft = "DRAFT";
    public const string StatusPendingApproval = "PENDING_APPROVAL";
    public const string StatusApproved = "APPROVED";
    public const string StatusRejected = "REJECTED";
    public const string StatusPaymentEligible = "PAYMENT_ELIGIBLE";
    public const string StatusPendingPayment = "PENDING_PAYMENT";
    public const string StatusPaid = "PAID";
    public const string StatusActive = "ACTIVE";

    public long Id { get; private set; }
    public long CompanyId { get; private set; }
    public long CustomerId { get; private set; }
    public string Status { get; private set; } = null!;
    public bool RequiresApproval { get; private set; }
    public long? WorkflowInstanceId { get; private set; }
    public long? ServiceId { get; private set; }
    public DateTime SaleDate { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public string? DiscountReason { get; private set; }
    public decimal TotalAmount { get; private set; }
    public long? PaymentTransactionId { get; private set; }
    public long? PreviousRequestId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private readonly List<CarePackageRequestItem> _items = new();
    public IReadOnlyCollection<CarePackageRequestItem> Items => _items.AsReadOnly();

    private CarePackageRequest() { }

    public static CarePackageRequest CreateDraft(
        long companyId,
        long customerId,
        long? serviceId,
        DateTime saleDate,
        long createdByUserId,
        long? previousRequestId = null)
    {
        return new CarePackageRequest
        {
            CompanyId = companyId,
            CustomerId = customerId,
            ServiceId = serviceId,
            SaleDate = saleDate,
            Status = StatusDraft,
            PreviousRequestId = previousRequestId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddItem(CarePackageRequestItem item)
    {
        if (Status != StatusDraft)
            throw new InvalidOperationException("Can only add items to draft requests.");

        _items.Add(item);
        RecalculateTotals();
    }

    public void SetDiscount(decimal discountAmount, string? discountReason)
    {
        if (Status != StatusDraft)
            throw new InvalidOperationException("Can only set discount on draft requests.");

        if (discountAmount < 0)
            throw new InvalidOperationException("Discount amount cannot be negative.");

        if (discountAmount > SubtotalAmount)
            throw new InvalidOperationException("Discount cannot exceed subtotal amount.");

        if (discountAmount > 0 && string.IsNullOrWhiteSpace(discountReason))
            throw new InvalidOperationException("Discount reason is required when discount is applied.");

        DiscountAmount = discountAmount;
        DiscountReason = discountReason;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        SubtotalAmount = 0;
        foreach (var item in _items)
        {
            SubtotalAmount += item.LineSubtotal;
        }

        if (DiscountAmount > SubtotalAmount)
        {
            DiscountAmount = SubtotalAmount;
        }

        TotalAmount = SubtotalAmount - DiscountAmount;
    }

    public void EvaluateApprovalRequirement()
    {
        if (Status != StatusDraft)
            throw new InvalidOperationException("Can only evaluate approval requirement on draft requests.");

        RequiresApproval = DiscountAmount > 0;
        // B1: More complex rules for changed-price renewal or overrides can be added here later
    }

    public void SetPaymentEligible()
    {
        if (Status != StatusDraft && Status != StatusApproved)
            throw new InvalidOperationException($"Cannot set payment eligible from status {Status}");
        
        if (Status == StatusDraft && RequiresApproval)
            throw new InvalidOperationException("Approval required requests cannot bypass approval to become payment eligible.");

        Status = StatusPaymentEligible;
        UpdatedAt = DateTime.UtcNow;
    }

    // Workflow / Payment transitions (To be fully fleshed out in B2, safe foundations here)
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

    public void SetPaymentDraft(long paymentTransactionId)
    {
        if (Status != StatusPaymentEligible)
            throw new InvalidOperationException($"Cannot set payment draft from status {Status}");

        Status = StatusPendingPayment;
        PaymentTransactionId = paymentTransactionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPaid()
    {
        if (Status != StatusPendingPayment)
            throw new InvalidOperationException($"Cannot mark as paid from status {Status}");

        Status = StatusPaid;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive()
    {
        if (Status != StatusPaid)
            throw new InvalidOperationException($"Cannot mark as active from status {Status}");

        Status = StatusActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
