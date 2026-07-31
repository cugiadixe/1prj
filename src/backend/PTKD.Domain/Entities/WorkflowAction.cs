using System;

namespace PTKD.Domain.Entities;

public class WorkflowAction
{
    public long Id { get; private set; }
    public long WorkflowInstanceStepId { get; private set; }
    public long WorkflowInstanceId { get; private set; }
    public string ActionType { get; private set; } = null!;
    public long ActedBy { get; private set; }
    public long? OnBehalfOf { get; private set; }
    public long? DelegationId { get; private set; }
    public string? Reason { get; private set; }
    public string? Comment { get; private set; }
    public Guid CorrelationId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WorkflowAction() { }

    public WorkflowAction(long workflowInstanceStepId, long workflowInstanceId, string actionType, long actedBy, string? reason = null, string? comment = null)
    {
        WorkflowInstanceStepId = workflowInstanceStepId;
        WorkflowInstanceId = workflowInstanceId;
        ActionType = actionType ?? throw new ArgumentNullException(nameof(actionType));
        ActedBy = actedBy;
        Reason = reason;
        Comment = comment;
        CorrelationId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
