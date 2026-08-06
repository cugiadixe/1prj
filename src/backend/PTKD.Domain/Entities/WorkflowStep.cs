using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class WorkflowStep
{
    public long Id { get; private set; }
    public long WorkflowVersionId { get; private set; }
    public int StepOrder { get; private set; }
    public string StepName { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsRequired { get; private set; }
    public int? DueDurationMinutes { get; private set; }
    public int? ReminderBeforeMinutes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public WorkflowDefinitionVersion Version { get; private set; } = null!;
    public ICollection<WorkflowStepApproverRule> ApproverRules { get; private set; } = new List<WorkflowStepApproverRule>();

    private WorkflowStep() { }

    public WorkflowStep(long workflowVersionId, int stepOrder, string stepName, bool isRequired = true, string? description = null)
    {
        WorkflowVersionId = workflowVersionId;
        StepOrder = stepOrder;
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        IsRequired = isRequired;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string stepName, int stepOrder, bool isRequired, string? description, int? dueDurationMinutes)
    {
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        StepOrder = stepOrder;
        IsRequired = isRequired;
        Description = description;
        DueDurationMinutes = dueDurationMinutes;
        UpdatedAt = DateTime.UtcNow;
    }
}
