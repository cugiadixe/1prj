using System;

namespace PTKD.Application.Organizations.Departments.DTOs;

public class DepartmentDto
{
    public long Id { get; set; }
    public string DepartmentCode { get; set; } = null!;
    public long CompanyId { get; set; }
    public long? ParentDepartmentId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
