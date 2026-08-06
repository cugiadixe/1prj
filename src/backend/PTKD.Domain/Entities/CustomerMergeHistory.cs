using System;

namespace PTKD.Domain.Entities;

public class CustomerMergeHistory
{
    public Guid Id { get; private set; }
    public Guid? MergeRequestId { get; private set; }
    public long SourceCustomerId { get; private set; }
    public long TargetCustomerId { get; private set; }
    public string ActionType { get; private set; } = null!;
    public long ActorId { get; private set; }
    public string SummaryPayload { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public virtual CustomerMergeRequest? MergeRequest { get; private set; }
    public virtual Customer SourceCustomer { get; private set; } = null!;
    public virtual Customer TargetCustomer { get; private set; } = null!;

    private CustomerMergeHistory() { }

    public CustomerMergeHistory(
        Guid? mergeRequestId,
        long sourceCustomerId,
        long targetCustomerId,
        string actionType,
        long actorId,
        string summaryPayload)
    {
        Id = Guid.NewGuid();
        MergeRequestId = mergeRequestId;
        SourceCustomerId = sourceCustomerId;
        TargetCustomerId = targetCustomerId;
        ActionType = actionType ?? throw new ArgumentNullException(nameof(actionType));
        ActorId = actorId;
        SummaryPayload = summaryPayload ?? throw new ArgumentNullException(nameof(summaryPayload));
        CreatedAt = DateTime.UtcNow;
    }
}
