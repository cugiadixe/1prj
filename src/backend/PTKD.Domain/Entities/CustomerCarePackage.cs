using System;

namespace PTKD.Domain.Entities;

public class CustomerCarePackage
{
    public const string StatusPendingGrave = "PENDING_GRAVE"; // đã gán khách, chờ gán mộ
    public const string StatusActive = "ACTIVE";              // đã gán mộ, hiệu lực
    public const string StatusExpired = "EXPIRED";            // hết hạn
    public const string StatusCancelled = "CANCELLED";        // đã hủy

    public long Id { get; private set; }
    public long CustomerId { get; private set; }
    public long ServiceTypeId { get; private set; }
    public long? GraveId { get; private set; }
    public int CotCount { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string Status { get; private set; } = null!;
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private CustomerCarePackage() { }

    public static CustomerCarePackage Create(
        long customerId, long serviceTypeId,
        int cotCount, decimal unitPrice, DateTime startDate, DateTime? endDate,
        string? notes, long createdByUserId)
    {
        if (cotCount <= 0)
            throw new ArgumentException("Cot count must be positive.", nameof(cotCount));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        return new CustomerCarePackage
        {
            CustomerId = customerId,
            ServiceTypeId = serviceTypeId,
            GraveId = null,
            CotCount = cotCount,
            UnitPrice = unitPrice,
            TotalPrice = unitPrice * cotCount,
            StartDate = startDate,
            EndDate = endDate,
            Status = StatusPendingGrave,
            Notes = notes,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AssignGrave(long graveId, long updatedByUserId)
    {
        if (Status == StatusCancelled)
            throw new InvalidOperationException("Cannot assign a grave to a cancelled package.");

        GraveId = graveId;
        Status = StatusActive;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    public void Cancel(long updatedByUserId)
    {
        Status = StatusCancelled;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
