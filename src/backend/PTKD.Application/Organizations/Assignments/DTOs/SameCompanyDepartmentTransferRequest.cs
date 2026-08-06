using System;
using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Assignments.DTOs;

public class SameCompanyDepartmentTransferRequest
{
    [Required]
    public string CompanyAssignmentRowVersion { get; set; } = null!;
    
    public long SourceDepartmentAssignmentId { get; set; }
    
    [Required]
    public string SourceDepartmentAssignmentRowVersion { get; set; } = null!;
    
    public long TargetDepartmentId { get; set; }
    
    public DateTime EffectiveDate { get; set; }
    
    public string? Reason { get; set; }
}
