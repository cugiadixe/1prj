using PTKD.Domain.ValueObjects;

namespace PTKD.Domain.Security.Authorization;

public class UserRoleAssignment
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public string AssignmentStatus { get; set; } = null!;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public RowVersion RowVersion { get; set; } = null!;
    
    public Role Role { get; set; } = null!;
}
