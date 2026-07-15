using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Users.DTOs;

public class UpdateUserRequest
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
    
    [Required]
    public string TargetVersion { get; set; } = null!;
}
