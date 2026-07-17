namespace PTKD.Domain.Security.Authorization;

public class AdminGroupPermission
{
    public long AdminGroupId { get; set; }
    public string PermissionCode { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}
