using System;

namespace PTKD.Application.Organizations.Assignments.DTOs;

public class AssignCompanyRequest
{
    public long CompanyId { get; set; }
    public long PrimaryDepartmentId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string? Reason { get; set; }
}
