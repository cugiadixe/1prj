using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Companies.DTOs;

public class UpdateCompanyStatusRequest
{
    public bool IsActive { get; set; }
    
    [Required]
    public string TargetVersion { get; set; } = null!;
}
