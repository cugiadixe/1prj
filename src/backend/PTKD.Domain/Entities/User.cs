using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class User
{
    public long Id { get; private set; }
    public string EmployeeCode { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string? Email { get; private set; }
    public string EmploymentStatus { get; private set; } = null!;
    public string AccountStatus { get; private set; } = null!;
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    // Navigation properties
    public ICollection<UserCompanyAssignment> CompanyAssignments { get; private set; } = new List<UserCompanyAssignment>();
    public ICollection<UserDepartmentAssignment> DepartmentAssignments { get; private set; } = new List<UserDepartmentAssignment>();
    public ICollection<EmploymentHistory> EmploymentHistories { get; private set; } = new List<EmploymentHistory>();

    private User() { } // EF Core

    public User(string employeeCode, string fullName, string? email, string employmentStatus, string accountStatus)
    {
        EmployeeCode = employeeCode ?? throw new ArgumentNullException(nameof(employeeCode));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Email = email;
        EmploymentStatus = employmentStatus ?? throw new ArgumentNullException(nameof(employmentStatus));
        AccountStatus = accountStatus ?? throw new ArgumentNullException(nameof(accountStatus));
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string employeeCode, string fullName, string? email, string employmentStatus, string accountStatus)
    {
        EmployeeCode = employeeCode ?? throw new ArgumentNullException(nameof(employeeCode));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Email = email;
        EmploymentStatus = employmentStatus ?? throw new ArgumentNullException(nameof(employmentStatus));
        AccountStatus = accountStatus ?? throw new ArgumentNullException(nameof(accountStatus));
        UpdatedAt = DateTime.UtcNow;
    }
}
