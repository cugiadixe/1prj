using System;

namespace PTKD.Domain.Entities;

public class WorkflowBinding
{
    public long Id { get; private set; }
    public long WorkflowVersionId { get; private set; }
    public string ProcessCode { get; private set; } = null!;
    public string ScopeType { get; private set; } = null!;
    public long? CompanyId { get; private set; }
    public int Priority { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public long CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public WorkflowDefinitionVersion Version { get; private set; } = null!;
    public BusinessProcessCatalog Process { get; private set; } = null!;

    private WorkflowBinding() { }

    public WorkflowBinding(long workflowVersionId, string processCode, string scopeType, DateTime effectiveFrom, long createdBy, long? companyId = null, int priority = 0, DateTime? effectiveTo = null)
    {
        WorkflowVersionId = workflowVersionId;
        ProcessCode = processCode ?? throw new ArgumentNullException(nameof(processCode));
        ScopeType = scopeType ?? throw new ArgumentNullException(nameof(scopeType));
        EffectiveFrom = effectiveFrom;
        CreatedBy = createdBy;
        CompanyId = companyId;
        Priority = priority;
        EffectiveTo = effectiveTo;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(DateTime effectiveFrom, DateTime? effectiveTo, int priority)
    {
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
