using PTKD.Domain.ValueObjects;

namespace PTKD.Domain.Security.Authorization;

public class AuthorizationPolicyState
{
    public int Id { get; set; }
    public long PolicyVersion { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public RowVersion RowVersion { get; set; } = null!;
}
