using System;
using System.Collections.Generic;
using PTKD.Domain.ValueObjects;

namespace PTKD.Domain.Entities;

public class UserCompanyAssignment
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public long CompanyId { get; private set; }
    public bool IsPrimary { get; private set; }
    public string AssignmentStatus { get; private set; } = null!;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public ICollection<UserDepartmentAssignment> DepartmentAssignments { get; private set; } = new List<UserDepartmentAssignment>();

    private UserCompanyAssignment() { } // EF Core

    public UserCompanyAssignment(long userId, long companyId, bool isPrimary, DateTime effectiveFrom)
    {
        UserId = userId;
        CompanyId = companyId;
        IsPrimary = isPrimary;
        AssignmentStatus = "ACTIVE";
        EffectiveFrom = effectiveFrom;
        CreatedAt = DateTime.UtcNow;
    }

    public AssignmentTimeline GetTimeline()
    {
        return AssignmentTimeline.Create(EffectiveFrom, EffectiveTo);
    }

    public void Close(DateTime effectiveTo)
    {
        if (AssignmentStatus != "ACTIVE")
            throw new InvalidOperationException("Only active assignments can be closed.");

        if (effectiveTo <= EffectiveFrom)
            throw new InvalidOperationException("EffectiveTo must be strictly greater than EffectiveFrom.");

        AssignmentStatus = "CLOSED";
        EffectiveTo = effectiveTo;
        IsPrimary = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrimary(bool isPrimary)
    {
        if (AssignmentStatus != "ACTIVE" && isPrimary)
            throw new InvalidOperationException("Closed assignments cannot be made primary.");

        IsPrimary = isPrimary;
        UpdatedAt = DateTime.UtcNow;
    }
}
