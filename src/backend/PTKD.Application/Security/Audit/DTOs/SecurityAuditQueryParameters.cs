using System;

namespace PTKD.Application.Security.Audit.DTOs;

public class SecurityAuditQueryParameters
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? EventType { get; set; }
    public long? ActorUserId { get; set; }
    public long? TargetUserId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public Guid? CorrelationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
