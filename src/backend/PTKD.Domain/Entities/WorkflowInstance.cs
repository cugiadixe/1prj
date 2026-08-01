using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class WorkflowInstance
{
    public long Id { get; private set; }
    public long WorkflowVersionId { get; private set; }
    public long WorkflowBindingId { get; private set; }
    public string ProcessCode { get; private set; } = null!;
    public long? CompanyId { get; private set; }
    public long RequesterId { get; private set; }
    public string BusinessEntityType { get; private set; } = null!;
    public long BusinessEntityId { get; private set; }
    public string InstanceStatus { get; private set; } = null!;
    public int RoundNo { get; private set; }
    public string WorkflowSnapshotJson { get; private set; } = null!;
    public string PayloadJson { get; private set; } = null!;
    public string PayloadHash { get; private set; } = null!;
    public Guid CorrelationId { get; private set; }
    public string? BeforeDataJson { get; private set; }
    public string? AfterDataJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public ICollection<WorkflowInstanceStep> Steps { get; private set; } = new List<WorkflowInstanceStep>();

    private WorkflowInstance() { }

    public WorkflowInstance(
        long workflowVersionId,
        long workflowBindingId,
        string processCode,
        long requesterId,
        string businessEntityType,
        long businessEntityId,
        string workflowSnapshotJson,
        string payloadJson,
        string payloadHash,
        long? companyId = null,
        string? beforeDataJson = null)
    {
        WorkflowVersionId = workflowVersionId;
        WorkflowBindingId = workflowBindingId;
        ProcessCode = processCode;
        RequesterId = requesterId;
        BusinessEntityType = businessEntityType;
        BusinessEntityId = businessEntityId;
        WorkflowSnapshotJson = workflowSnapshotJson;
        PayloadJson = payloadJson;
        PayloadHash = payloadHash;
        CompanyId = companyId;
        BeforeDataJson = beforeDataJson;
        InstanceStatus = "PENDING_APPROVAL";
        RoundNo = 1;
        CorrelationId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public void SetApproved()
    {
        InstanceStatus = "APPROVED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetReturned()
    {
        InstanceStatus = "RETURNED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetWithdrawn()
    {
        InstanceStatus = "WITHDRAWN";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRejected()
    {
        InstanceStatus = "REJECTED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPendingExecution()
    {
        InstanceStatus = "PENDING_EXECUTION";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetExecuting()
    {
        InstanceStatus = "EXECUTING";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetExecuted(string? afterDataJson)
    {
        InstanceStatus = "EXECUTED";
        AfterDataJson = afterDataJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFailed()
    {
        InstanceStatus = "FAILED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resubmit()
    {
        RoundNo++;
        InstanceStatus = "PENDING_APPROVAL";
        UpdatedAt = DateTime.UtcNow;
    }
}
