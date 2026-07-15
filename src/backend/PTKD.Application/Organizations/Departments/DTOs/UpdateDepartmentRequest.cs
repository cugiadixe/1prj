using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Departments.DTOs;

public class UpdateDepartmentRequest
{
    [Required]
    public string DepartmentCode { get; set; } = null!;
    
    public long? ParentDepartmentId { get; set; }
    
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string TargetVersion { get; set; } = null!;
}
