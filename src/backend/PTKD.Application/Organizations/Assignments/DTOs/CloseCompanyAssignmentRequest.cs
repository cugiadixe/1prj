using System;
using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Assignments.DTOs;

public class CloseCompanyAssignmentRequest
{
    [Required]
    public string CompanyAssignmentRowVersion { get; set; } = null!;
    
    public long? ReplacementPrimaryCompanyAssignmentId { get; set; }
    
    public string? ReplacementPrimaryCompanyRowVersion { get; set; }
    
    public DateTime EffectiveTo { get; set; }
    
    public string? Reason { get; set; }
}
