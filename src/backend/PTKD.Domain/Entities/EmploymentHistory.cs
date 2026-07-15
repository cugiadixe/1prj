using System;

namespace PTKD.Domain.Entities;

public class EmploymentHistory
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    
    public long? FromCompanyId { get; private set; }
    public long? ToCompanyId { get; private set; }
    
    public long? FromDepartmentId { get; private set; }
    public long? ToDepartmentId { get; private set; }
    
    public long? FromCompanyAssignmentId { get; private set; }
    public long? ToCompanyAssignmentId { get; private set; }
    
    public long? FromDepartmentAssignmentId { get; private set; }
    public long? ToDepartmentAssignmentId { get; private set; }
    
    public string ActionType { get; private set; } = null!;
    public string? Reason { get; private set; }
    
    public DateTime EffectiveDate { get; private set; }
    public Guid? CorrelationId { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;

    private EmploymentHistory() { } // EF Core

    public EmploymentHistory(
        long userId,
        string actionType,
        DateTime effectiveDate,
        string? reason = null,
        Guid? correlationId = null,
        long? fromCompanyId = null,
        long? toCompanyId = null,
        long? fromDepartmentId = null,
        long? toDepartmentId = null,
        long? fromCompanyAssignmentId = null,
        long? toCompanyAssignmentId = null,
        long? fromDepartmentAssignmentId = null,
        long? toDepartmentAssignmentId = null)
    {
        UserId = userId;
        ActionType = actionType ?? throw new ArgumentNullException(nameof(actionType));
        EffectiveDate = effectiveDate;
        Reason = reason;
        CorrelationId = correlationId;
        
        FromCompanyId = fromCompanyId;
        ToCompanyId = toCompanyId;
        FromDepartmentId = fromDepartmentId;
        ToDepartmentId = toDepartmentId;
        
        FromCompanyAssignmentId = fromCompanyAssignmentId;
        ToCompanyAssignmentId = toCompanyAssignmentId;
        FromDepartmentAssignmentId = fromDepartmentAssignmentId;
        ToDepartmentAssignmentId = toDepartmentAssignmentId;
        
        CreatedAt = DateTime.UtcNow;
    }
}
