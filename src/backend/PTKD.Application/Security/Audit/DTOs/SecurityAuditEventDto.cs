using System;

namespace PTKD.Application.Security.Audit.DTOs;

public class SecurityAuditEventDto
{
    public long Id { get; set; }
    public long? ActorUserId { get; set; }
    public long? ActingAsUserId { get; set; }
    public long? TargetUserId { get; set; }
    public long? CompanyId { get; set; }
    public string EventCode { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Reason { get; set; }
    public Guid CorrelationId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public long? PolicyVersion { get; set; }
    public DateTime CreatedAt { get; set; }
}
