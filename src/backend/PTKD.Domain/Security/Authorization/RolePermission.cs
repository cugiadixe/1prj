namespace PTKD.Domain.Security.Authorization;

public class RolePermission
{
    public long RoleId { get; set; }
    public string PermissionCode { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}
