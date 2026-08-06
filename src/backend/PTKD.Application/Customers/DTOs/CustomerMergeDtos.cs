using System;
using System.Collections.Generic;

namespace PTKD.Application.Customers.DTOs;

public class CreateCustomerMergeRequestDto
{
    public long SourceCustomerId { get; set; }
    public long TargetCustomerId { get; set; }
    public string SurvivorshipPayload { get; set; } = null!;
    public string SourceRowVersionSnapshot { get; set; } = null!;
    public string TargetRowVersionSnapshot { get; set; } = null!;

    public List<CustomerMergeCandidateDto> Candidates { get; set; } = new();
}

public class CustomerMergeCandidateDto
{
    public long CandidateCustomerId { get; set; }
    public string MatchType { get; set; } = null!;
    public decimal? MatchConfidence { get; set; }
    public string? SnapshotPayload { get; set; }
}

public class CustomerMergeRequestDto
{
    public Guid Id { get; set; }
    public long SourceCustomerId { get; set; }
    public long TargetCustomerId { get; set; }
    public long RequesterId { get; set; }
    public string RequestStatus { get; set; } = null!;
    public string SurvivorshipPayload { get; set; } = null!;
    public string SourceRowVersionSnapshot { get; set; } = null!;
    public string TargetRowVersionSnapshot { get; set; } = null!;
    public long? WorkflowInstanceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = null!;

    public List<CustomerMergeCandidateDto> Candidates { get; set; } = new();
}
