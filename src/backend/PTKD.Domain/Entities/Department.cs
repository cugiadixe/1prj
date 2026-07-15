using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class Department
{
    public long Id { get; private set; }
    public string DepartmentCode { get; private set; } = null!;
    public long CompanyId { get; private set; }
    public long? ParentDepartmentId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    // Navigation properties
    public Company Company { get; private set; } = null!;
    public Department? ParentDepartment { get; private set; }
    public ICollection<Department> ChildDepartments { get; private set; } = new List<Department>();
    public ICollection<UserDepartmentAssignment> UserAssignments { get; private set; } = new List<UserDepartmentAssignment>();

    private Department() { } // EF Core

    public Department(string departmentCode, long companyId, long? parentDepartmentId, string name)
    {
        DepartmentCode = departmentCode ?? throw new ArgumentNullException(nameof(departmentCode));
        CompanyId = companyId;
        ParentDepartmentId = parentDepartmentId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string departmentCode, long? parentDepartmentId, string name)
    {
        DepartmentCode = departmentCode ?? throw new ArgumentNullException(nameof(departmentCode));
        ParentDepartmentId = parentDepartmentId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
