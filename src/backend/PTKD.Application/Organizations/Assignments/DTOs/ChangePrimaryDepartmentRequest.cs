using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Assignments.DTOs;

public class ChangePrimaryDepartmentRequest
{
    [Required]
    public string TargetRowVersion { get; set; } = null!;
    
    public long CurrentPrimaryAssignmentId { get; set; }
    
    [Required]
    public string CurrentPrimaryRowVersion { get; set; } = null!;
    
    public string? Reason { get; set; }
}
