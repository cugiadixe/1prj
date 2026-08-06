using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class Company
{
    public long Id { get; private set; }
    public string CompanyCode { get; private set; } = null!;
    public long? ParentCompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? TaxCode { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    
    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }
    
    // Navigation properties
    public Company? ParentCompany { get; private set; }
    public ICollection<Company> ChildCompanies { get; private set; } = new List<Company>();
    public ICollection<Department> Departments { get; private set; } = new List<Department>();
    public ICollection<UserCompanyAssignment> UserAssignments { get; private set; } = new List<UserCompanyAssignment>();

    private Company() { } // EF Core

    public Company(string companyCode, long? parentCompanyId, string name, string? taxCode)
    {
        CompanyCode = companyCode ?? throw new ArgumentNullException(nameof(companyCode));
        ParentCompanyId = parentCompanyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TaxCode = taxCode;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string companyCode, long? parentCompanyId, string name, string? taxCode)
    {
        CompanyCode = companyCode ?? throw new ArgumentNullException(nameof(companyCode));
        ParentCompanyId = parentCompanyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TaxCode = taxCode;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
