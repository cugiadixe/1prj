using System;

namespace PTKD.Application.Organizations.Companies.DTOs;

public class CompanyDto
{
    public long Id { get; set; }
    public string CompanyCode { get; set; } = null!;
    public long? ParentCompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string? TaxCode { get; set; }
    public bool IsActive { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
