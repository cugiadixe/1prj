using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class WorkflowDefinition
{
    public long Id { get; private set; }
    public string DefinitionCode { get; private set; } = null!;
    public string DefinitionName { get; private set; } = null!;
    public string? Description { get; private set; }
    public string ProcessCode { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public long CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public BusinessProcessCatalog Process { get; private set; } = null!;
    public ICollection<WorkflowDefinitionVersion> Versions { get; private set; } = new List<WorkflowDefinitionVersion>();

    private WorkflowDefinition() { }

    public WorkflowDefinition(string definitionCode, string definitionName, string processCode, long createdBy, string? description = null)
    {
        DefinitionCode = definitionCode ?? throw new ArgumentNullException(nameof(definitionCode));
        DefinitionName = definitionName ?? throw new ArgumentNullException(nameof(definitionName));
        ProcessCode = processCode ?? throw new ArgumentNullException(nameof(processCode));
        CreatedBy = createdBy;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string definitionName, string? description, long updatedByUserId)
    {
        DefinitionName = definitionName ?? throw new ArgumentNullException(nameof(definitionName));
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
