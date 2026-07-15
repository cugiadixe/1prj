using System;
using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Assignments.DTOs;

public class CrossCompanyTransferRequest
{
    [Required]
    public string SourceCompanyAssignmentRowVersion { get; set; } = null!;
    
    public long TargetCompanyId { get; set; }
    
    public long TargetDepartmentId { get; set; }
    
    public bool MakeTargetPrimaryCompany { get; set; }
    
    public long? ReplacementPrimaryCompanyAssignmentId { get; set; }
    
    public string? ReplacementPrimaryCompanyRowVersion { get; set; }
    
    public DateTime EffectiveDate { get; set; }
    
    public string? Reason { get; set; }
}
