using System;

namespace PTKD.Domain.Entities;

public class CustomerMergeRequestCandidate
{
    public Guid Id { get; private set; }
    public Guid MergeRequestId { get; private set; }
    public long CandidateCustomerId { get; private set; }
    public string MatchType { get; private set; } = null!;
    public decimal? MatchConfidence { get; private set; }
    public string? SnapshotPayload { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public virtual CustomerMergeRequest MergeRequest { get; private set; } = null!;
    public virtual Customer CandidateCustomer { get; private set; } = null!;

    private CustomerMergeRequestCandidate() { }

    public CustomerMergeRequestCandidate(
        Guid mergeRequestId,
        long candidateCustomerId,
        string matchType,
        decimal? matchConfidence,
        string? snapshotPayload)
    {
        Id = Guid.NewGuid();
        MergeRequestId = mergeRequestId;
        CandidateCustomerId = candidateCustomerId;
        MatchType = matchType ?? throw new ArgumentNullException(nameof(matchType));
        MatchConfidence = matchConfidence;
        SnapshotPayload = snapshotPayload;
        CreatedAt = DateTime.UtcNow;
    }
}
