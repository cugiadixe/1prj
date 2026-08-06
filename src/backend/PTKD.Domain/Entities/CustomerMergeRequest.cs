using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class CustomerMergeRequest
{
    public Guid Id { get; private set; }
    public long SourceCustomerId { get; private set; }
    public long TargetCustomerId { get; private set; }
    public long RequesterId { get; private set; }
    public string RequestStatus { get; private set; } = null!;
    public string SurvivorshipPayload { get; private set; } = null!;
    public byte[] SourceRowVersionSnapshot { get; private set; } = null!;
    public byte[] TargetRowVersionSnapshot { get; private set; } = null!;
    public long? WorkflowInstanceId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public virtual ICollection<CustomerMergeRequestCandidate> Candidates { get; private set; } = new List<CustomerMergeRequestCandidate>();
    public virtual Customer SourceCustomer { get; private set; } = null!;
    public virtual Customer TargetCustomer { get; private set; } = null!;

    private CustomerMergeRequest() { }

    public CustomerMergeRequest(
        long sourceCustomerId,
        long targetCustomerId,
        long requesterId,
        string survivorshipPayload,
        byte[] sourceRowVersionSnapshot,
        byte[] targetRowVersionSnapshot)
    {
        if (sourceCustomerId == targetCustomerId)
            throw new ArgumentException("Source and target customer cannot be the same.");

        Id = Guid.NewGuid();
        SourceCustomerId = sourceCustomerId;
        TargetCustomerId = targetCustomerId;
        RequesterId = requesterId;
        SurvivorshipPayload = survivorshipPayload ?? throw new ArgumentNullException(nameof(survivorshipPayload));
        SourceRowVersionSnapshot = sourceRowVersionSnapshot ?? throw new ArgumentNullException(nameof(sourceRowVersionSnapshot));
        TargetRowVersionSnapshot = targetRowVersionSnapshot ?? throw new ArgumentNullException(nameof(targetRowVersionSnapshot));
        RequestStatus = "DRAFT";
        CreatedAt = DateTime.UtcNow;
    }

    public void AddCandidate(long candidateCustomerId, string matchType, decimal? matchConfidence, string? snapshotPayload)
    {
        Candidates.Add(new CustomerMergeRequestCandidate(Id, candidateCustomerId, matchType, matchConfidence, snapshotPayload));
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

    public void SetExecuted()
    {
        RequestStatus = "EXECUTED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRejected()
    {
        RequestStatus = "REJECTED";
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetWithdrawn()
    {
        RequestStatus = "WITHDRAWN";
        UpdatedAt = DateTime.UtcNow;
    }
}
