using PTKD.Domain.ValueObjects;

namespace PTKD.Domain.Security.Authorization;

public class AdminGroup
{
    public long Id { get; set; }
    public string GroupCode { get; set; } = null!;
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
    
    public ICollection<AdminGroupPermission> Permissions { get; set; } = new List<AdminGroupPermission>();
}
