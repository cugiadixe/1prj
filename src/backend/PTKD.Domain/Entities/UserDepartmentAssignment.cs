using System;
using PTKD.Domain.ValueObjects;

namespace PTKD.Domain.Entities;

public class UserDepartmentAssignment
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public long DepartmentId { get; private set; }
    public long UserCompanyAssignmentId { get; private set; }
    public long CompanyId { get; private set; }
    public bool IsPrimaryForCompany { get; private set; }
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
    public Department Department { get; private set; } = null!;
    public UserCompanyAssignment UserCompanyAssignment { get; private set; } = null!;

    private UserDepartmentAssignment() { } // EF Core

    public UserDepartmentAssignment(long userId, long departmentId, long userCompanyAssignmentId, long companyId, bool isPrimaryForCompany, DateTime effectiveFrom)
    {
        UserId = userId;
        DepartmentId = departmentId;
        UserCompanyAssignmentId = userCompanyAssignmentId;
        CompanyId = companyId;
        IsPrimaryForCompany = isPrimaryForCompany;
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
        IsPrimaryForCompany = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrimary(bool isPrimary)
    {
        if (AssignmentStatus != "ACTIVE" && isPrimary)
            throw new InvalidOperationException("Closed assignments cannot be made primary.");

        IsPrimaryForCompany = isPrimary;
        UpdatedAt = DateTime.UtcNow;
    }
}
