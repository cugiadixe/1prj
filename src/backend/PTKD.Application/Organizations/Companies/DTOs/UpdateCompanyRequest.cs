using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Companies.DTOs;

public class UpdateCompanyRequest
{
    [Required]
    public string CompanyCode { get; set; } = null!;
    
    public long? ParentCompanyId { get; set; }
    
    [Required]
    public string Name { get; set; } = null!;
    
    public string? TaxCode { get; set; }

    [Required]
    public string TargetVersion { get; set; } = null!;
}
