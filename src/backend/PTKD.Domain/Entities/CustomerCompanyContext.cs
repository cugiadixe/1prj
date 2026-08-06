using System;

namespace PTKD.Domain.Entities;

public class CustomerCompanyContext
{
    public long Id { get; private set; }
    public long CustomerId { get; private set; }
    public long CompanyId { get; private set; }
    public long? AssignedStaffId { get; private set; }
    public string RelationshipStatus { get; private set; } = null!;
    public string? InternalNotes { get; private set; }
    public DateTime? FirstInteractionAt { get; private set; }
    public DateTime? LastInteractionAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private CustomerCompanyContext() { }

    public CustomerCompanyContext(long customerId, long companyId, long? assignedStaffId,
        string? internalNotes, DateTime? firstInteractionAt)
    {
        CustomerId = customerId;
        CompanyId = companyId;
        AssignedStaffId = assignedStaffId;
        RelationshipStatus = "ACTIVE";
        InternalNotes = internalNotes;
        FirstInteractionAt = firstInteractionAt;
        LastInteractionAt = firstInteractionAt;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetCreatedBy(long userId)
    {
        CreatedByUserId = userId;
    }

    public void Update(long? assignedStaffId, string relationshipStatus,
        string? internalNotes, DateTime? lastInteractionAt, long? updatedByUserId)
    {
        AssignedStaffId = assignedStaffId;
        RelationshipStatus = relationshipStatus;
        InternalNotes = internalNotes;
        if (lastInteractionAt.HasValue)
            LastInteractionAt = lastInteractionAt;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
