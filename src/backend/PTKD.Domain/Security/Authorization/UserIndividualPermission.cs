using PTKD.Domain.ValueObjects;

namespace PTKD.Domain.Security.Authorization;

public class UserIndividualPermission
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string PermissionCode { get; set; } = null!;
    public string ScopeType { get; set; } = null!;
    public long? CompanyId { get; set; }
    public string GrantType { get; set; } = null!;
    public string AssignmentStatus { get; set; } = null!;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Reason { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public RowVersion RowVersion { get; set; } = null!;
}
