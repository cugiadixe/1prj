using System;

namespace PTKD.Domain.Entities;

public class Service
{
    public const string StatusActive = "ACTIVE";
    public const string StatusExpired = "EXPIRED";
    public const string StatusCancelled = "CANCELLED";
    public const string StatusPendingPriceOverride = "PENDING_PRICE_OVERRIDE";

    public long Id { get; private set; }
    public long ServiceTypeId { get; private set; }
    public long CustomerId { get; private set; }
    public long CompanyId { get; private set; }
    public string Status { get; private set; } = null!;
    public decimal AppliedPrice { get; private set; }
    public decimal StandardPriceSnapshot { get; private set; }
    public bool IsOverridePrice { get; private set; }
    public long? OverrideApprovalRequestId { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public int CycleNumber { get; private set; }
    public long? PreviousServiceId { get; private set; }
    public long CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private Service() { }

    public static Service CreateStandard(
        long serviceTypeId,
        long customerId,
        long companyId,
        decimal standardPrice,
        DateTime validFrom,
        DateTime? validTo,
        long createdByUserId)
    {
        if (standardPrice <= 0)
            throw new ArgumentException("Standard price must be greater than zero.", nameof(standardPrice));

        return new Service
        {
            ServiceTypeId = serviceTypeId,
            CustomerId = customerId,
            CompanyId = companyId,
            Status = StatusActive,
            AppliedPrice = standardPrice,
            StandardPriceSnapshot = standardPrice,
            IsOverridePrice = false,
            ValidFrom = validFrom,
            ValidTo = validTo,
            CycleNumber = 1,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Service CreateRenewal(
        long serviceTypeId,
        long customerId,
        long companyId,
        decimal standardPrice,
        DateTime validFrom,
        DateTime? validTo,
        int cycleNumber,
        long previousServiceId,
        long createdByUserId)
    {
        if (standardPrice <= 0)
            throw new ArgumentException("Standard price must be greater than zero.", nameof(standardPrice));
        if (cycleNumber <= 0)
            throw new ArgumentException("Cycle number must be greater than zero.", nameof(cycleNumber));

        return new Service
        {
            ServiceTypeId = serviceTypeId,
            CustomerId = customerId,
            CompanyId = companyId,
            Status = StatusActive,
            AppliedPrice = standardPrice,
            StandardPriceSnapshot = standardPrice,
            IsOverridePrice = false,
            ValidFrom = validFrom,
            ValidTo = validTo,
            CycleNumber = cycleNumber,
            PreviousServiceId = previousServiceId,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Expire()
    {
        if (Status != StatusActive)
            throw new InvalidOperationException($"Cannot expire a service in {Status} status.");

        Status = StatusExpired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status != StatusActive && Status != StatusPendingPriceOverride)
            throw new InvalidOperationException($"Cannot cancel a service in {Status} status.");

        Status = StatusCancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPendingPriceOverride()
    {
        if (Status != StatusActive)
            throw new InvalidOperationException($"Cannot request price override for a service in {Status} status.");

        Status = StatusPendingPriceOverride;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyPriceOverride(decimal newPrice, long approvalRequestId)
    {
        if (Status != StatusPendingPriceOverride)
            throw new InvalidOperationException($"Cannot apply price override for a service in {Status} status.");
        if (newPrice <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(newPrice));

        AppliedPrice = newPrice;
        IsOverridePrice = true;
        OverrideApprovalRequestId = approvalRequestId;
        Status = StatusActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevertPendingOverride()
    {
        if (Status != StatusPendingPriceOverride)
            throw new InvalidOperationException($"Cannot revert override for a service in {Status} status.");

        Status = StatusActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
