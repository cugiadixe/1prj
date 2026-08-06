using System;
using System.Text.RegularExpressions;

namespace PTKD.Application.Security.Audit;

// Write-record for Security_Audit_Events. Not a tracked EF entity (OD-F-01).
// Callers are responsible for not placing passwords, tokens, or signing keys
// in any JSON field (SEC-005). ThrowIfContainsSensitiveData enforces this at write time.
public sealed record SecurityAuditEventRecord
{
    private static readonly Regex SensitiveKeyRegex = new(
        @"""(?:password|token|secret|signing_key|private_key|api_key|auth_key|access_key)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public required string EventCode { get; init; }
    public required string EntityType { get; init; }
    public required string Outcome { get; init; }
    public required Guid CorrelationId { get; init; }

    public long? ActorUserId { get; init; }
    public long? ActingAsUserId { get; init; }
    public long? TargetUserId { get; init; }
    public long? CompanyId { get; init; }
    public string? EntityId { get; init; }
    public string? ChangedFieldsJson { get; init; }
    public string? BeforeStateJson { get; init; }
    public string? AfterStateJson { get; init; }
    public string? Reason { get; init; }
    public string? RequestMetadataJson { get; init; }
    public long? PolicyVersion { get; init; }

    // Enforces SEC-005: sensitive key patterns must not appear as JSON property names
    // in any JSON field. Throws ArgumentException if a blocked key is found.
    public void ThrowIfContainsSensitiveData()
    {
        CheckField(ChangedFieldsJson, nameof(ChangedFieldsJson));
        CheckField(BeforeStateJson, nameof(BeforeStateJson));
        CheckField(AfterStateJson, nameof(AfterStateJson));
        CheckField(RequestMetadataJson, nameof(RequestMetadataJson));
    }

    private static void CheckField(string? json, string fieldName)
    {
        if (json is null) return;
        var match = SensitiveKeyRegex.Match(json);
        if (match.Success)
            throw new ArgumentException(
                $"Audit field '{fieldName}' contains a blocked JSON key '{match.Value}' (SEC-005).",
                fieldName);
    }
}
