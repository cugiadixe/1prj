using System;

namespace PTKD.Domain.Entities;

public class CustomerChangeRequest
{
    public long Id { get; private set; }
    public string ProcessCode { get; private set; } = null!;
    public long RequesterId { get; private set; }
    public long? CompanyId { get; private set; }
    public string RequestStatus { get; private set; } = null!;
    public string PayloadJson { get; private set; } = null!;
    public long? WorkflowInstanceId { get; private set; }
    public long? CreatedCustomerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private CustomerChangeRequest() { }

    public CustomerChangeRequest(string processCode, long requesterId, string payloadJson, long? companyId = null)
    {
        ProcessCode = processCode ?? throw new ArgumentNullException(nameof(processCode));
        RequesterId = requesterId;
        PayloadJson = payloadJson ?? throw new ArgumentNullException(nameof(payloadJson));
        CompanyId = companyId;
        RequestStatus = "DRAFT";
        CreatedAt = DateTime.UtcNow;
    }

    public void SetSubmitted(long workflowInstanceId)
    {
        WorkflowInstanceId = workflowInstanceId;
        RequestStatus = "SUBMITTED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetApproved()
    {
        RequestStatus = "APPROVED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetExecuted(long createdCustomerId)
    {
        CreatedCustomerId = createdCustomerId;
        RequestStatus = "EXECUTED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFailed()
    {
        RequestStatus = "FAILED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetWithdrawn()
    {
        RequestStatus = "WITHDRAWN";
        UpdatedAt = DateTime.UtcNow;
    }
}
