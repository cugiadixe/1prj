using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class WorkflowInstanceStep
{
    public long Id { get; private set; }
    public long WorkflowInstanceId { get; private set; }
    public long WorkflowStepId { get; private set; }
    public int StepOrder { get; private set; }
    public string StepName { get; private set; } = null!;
    public int RoundNo { get; private set; }
    public string StepStatus { get; private set; } = null!;
    public bool IsOverdue { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public long? CompletedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public WorkflowInstance Instance { get; private set; } = null!;
    public ICollection<WorkflowInstanceStepAssignee> Assignees { get; private set; } = new List<WorkflowInstanceStepAssignee>();

    private WorkflowInstanceStep() { }

    public WorkflowInstanceStep(long workflowInstanceId, long workflowStepId, int stepOrder, string stepName, int roundNo, string stepStatus)
    {
        WorkflowInstanceId = workflowInstanceId;
        WorkflowStepId = workflowStepId;
        StepOrder = stepOrder;
        StepName = stepName;
        RoundNo = roundNo;
        StepStatus = stepStatus;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetPending()
    {
        StepStatus = "PENDING";
        AssignedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetApproved(long completedBy)
    {
        StepStatus = "APPROVED";
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetReturned(long completedBy)
    {
        StepStatus = "RETURNED";
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCancelled()
    {
        StepStatus = "CANCELLED";
        UpdatedAt = DateTime.UtcNow;
    }
}
