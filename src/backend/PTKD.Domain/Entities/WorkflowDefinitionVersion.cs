using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class WorkflowDefinitionVersion
{
    public long Id { get; private set; }
    public long WorkflowDefinitionId { get; private set; }
    public int VersionNumber { get; private set; }
    public string VersionStatus { get; private set; } = null!;
    public DateTime? EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public long? PublishedBy { get; private set; }
    public long CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public WorkflowDefinition Definition { get; private set; } = null!;
    public ICollection<WorkflowStep> Steps { get; private set; } = new List<WorkflowStep>();
    public ICollection<WorkflowCondition> Conditions { get; private set; } = new List<WorkflowCondition>();

    private WorkflowDefinitionVersion() { }

    public WorkflowDefinitionVersion(long workflowDefinitionId, int versionNumber, long createdBy)
    {
        WorkflowDefinitionId = workflowDefinitionId;
        VersionNumber = versionNumber;
        VersionStatus = "DRAFT";
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    public void Publish(long publishedBy, DateTime effectiveFrom, DateTime? effectiveTo = null)
    {
        VersionStatus = "PUBLISHED";
        PublishedAt = DateTime.UtcNow;
        PublishedBy = publishedBy;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        VersionStatus = "ACTIVE";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Retire()
    {
        VersionStatus = "RETIRED";
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDraft => VersionStatus == "DRAFT";
    public bool IsPublished => VersionStatus == "PUBLISHED";
    public bool IsActive => VersionStatus == "ACTIVE";
}
