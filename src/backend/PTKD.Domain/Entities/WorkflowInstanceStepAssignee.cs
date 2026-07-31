using System;

namespace PTKD.Domain.Entities;

public class WorkflowInstanceStepAssignee
{
    public long Id { get; private set; }
    public long WorkflowInstanceStepId { get; private set; }
    public long UserId { get; private set; }
    public string ApproverSourceType { get; private set; } = null!;
    public bool IsResolved { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public WorkflowInstanceStep Step { get; private set; } = null!;

    private WorkflowInstanceStepAssignee() { }

    public WorkflowInstanceStepAssignee(long workflowInstanceStepId, long userId, string approverSourceType)
    {
        WorkflowInstanceStepId = workflowInstanceStepId;
        UserId = userId;
        ApproverSourceType = approverSourceType;
        IsResolved = true;
        CreatedAt = DateTime.UtcNow;
    }
}
