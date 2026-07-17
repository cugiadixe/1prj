using PTKD.Domain.ValueObjects;

namespace PTKD.Domain.Security.Authorization;

public class Role
{
    public long Id { get; set; }
    public string RoleCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ScopeType { get; set; } = null!;
    public long? CompanyId { get; set; }
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public RowVersion RowVersion { get; set; } = null!;
    
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
}
