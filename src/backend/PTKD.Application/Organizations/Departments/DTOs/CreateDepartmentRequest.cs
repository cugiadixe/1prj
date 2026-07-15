using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Departments.DTOs;

public class CreateDepartmentRequest
{
    [Required]
    public string DepartmentCode { get; set; } = null!;
    
    public long CompanyId { get; set; }
    
    public long? ParentDepartmentId { get; set; }
    
    [Required]
    public string Name { get; set; } = null!;
}
