using PTKD.Domain.ValueObjects;

namespace PTKD.Domain.Security.Authorization;

public class Permission
{
    public string PermissionCode { get; set; } = null!;
    public string ModuleCode { get; set; } = null!;
    public string ActionCode { get; set; } = null!;
    public string DataScope { get; set; } = null!;
    public bool IsSensitive { get; set; }
    public bool RequiresReason { get; set; }
    public bool IsDelegable { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public RowVersion RowVersion { get; set; } = null!;
}
