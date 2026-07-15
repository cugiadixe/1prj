using System;
using System.ComponentModel.DataAnnotations;

namespace PTKD.Application.Organizations.Assignments.DTOs;

public class AssignDepartmentRequest
{
    public long UserCompanyAssignmentId { get; set; }
    
    [Required]
    public string CompanyAssignmentRowVersion { get; set; } = null!;
    
    public long DepartmentId { get; set; }
    
    public DateTime EffectiveFrom { get; set; }
    
    public string? Reason { get; set; }
}
