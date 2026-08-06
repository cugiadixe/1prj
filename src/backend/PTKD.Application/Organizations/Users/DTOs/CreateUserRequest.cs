using System;
using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Users.DTOs;

public class CreateUserRequest
{
    [Required]
    public string EmployeeCode { get; set; } = null!;
    
    [Required]
    public string FullName { get; set; } = null!;
    
    public string? Email { get; set; }
    
    [Required]
    public string EmploymentStatus { get; set; } = null!;
    
    [Required]
    public string AccountStatus { get; set; } = null!;
    
    public long InitialCompanyId { get; set; }
    
    public long InitialDepartmentId { get; set; }
    
    public DateTime EffectiveFrom { get; set; }
    
    public string? Reason { get; set; }
}
