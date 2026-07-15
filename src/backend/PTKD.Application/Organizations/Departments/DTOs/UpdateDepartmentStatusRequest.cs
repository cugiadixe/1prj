using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Departments.DTOs;

public class UpdateDepartmentStatusRequest
{
    public bool IsActive { get; set; }
    
    [Required]
    public string TargetVersion { get; set; } = null!;
}
