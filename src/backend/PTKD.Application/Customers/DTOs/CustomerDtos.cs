using System;
using PTKD.Application.Tags.DTOs;

namespace PTKD.Application.Customers.DTOs;

public class CustomerListItemDto
{
    public long Id { get; set; }
    public string CustomerCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Cccd { get; set; }
    public string? Phone { get; set; }
    public string CustomerStatus { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public TagDto[] Tags { get; set; } = Array.Empty<TagDto>();
}

public class CustomerDetailDto
{
    public long Id { get; set; }
    public string CustomerCode { get; set; } = null!;
    public string CustomerStatus { get; set; } = null!;
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ProfileDto Profile { get; set; } = null!;
    public TagDto[] Tags { get; set; } = Array.Empty<TagDto>();
}

public class ProfileDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Cccd { get; set; }
    public DateTime? Dob { get; set; }
    public string? DobPartial { get; set; }
    public string? DobPrecision { get; set; }
    public string? Gender { get; set; }
    public string? PermanentAddress { get; set; }
    public DateTime? CccdIssueDate { get; set; }
    public string? CccdIssuePlace { get; set; }
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? ContactAddress { get; set; }
    public DateTime? DeathDateSolar { get; set; }
    public string? DeathDateLunar { get; set; }
    public string? DeathPlace { get; set; }
    public string? Hometown { get; set; }
    public bool IsActive { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class CreateCustomerRequest
{
    public string CustomerCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Cccd { get; set; }
    public DateTime? Dob { get; set; }
    public string? DobPartial { get; set; }
    public string? DobPrecision { get; set; }
    public string? Gender { get; set; }
    public string? PermanentAddress { get; set; }
    public DateTime? CccdIssueDate { get; set; }
    public string? CccdIssuePlace { get; set; }
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? ContactAddress { get; set; }
    public DateTime? DeathDateSolar { get; set; }
    public string? DeathDateLunar { get; set; }
    public string? DeathPlace { get; set; }
    public string? Hometown { get; set; }

    public long? InitialCompanyId { get; set; }
    public long? AssignedStaffId { get; set; }
    public string? InternalNotes { get; set; }
}

public class UpdateCustomerRequest
{
    public string FullName { get; set; } = null!;
    public string? Cccd { get; set; }
    public DateTime? Dob { get; set; }
    public string? DobPartial { get; set; }
    public string? DobPrecision { get; set; }
    public string? Gender { get; set; }
    public string? PermanentAddress { get; set; }
    public DateTime? CccdIssueDate { get; set; }
    public string? CccdIssuePlace { get; set; }
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? ContactAddress { get; set; }
    public DateTime? DeathDateSolar { get; set; }
    public string? DeathDateLunar { get; set; }
    public string? DeathPlace { get; set; }
    public string? Hometown { get; set; }

    public string Reason { get; set; } = null!;
    public string TargetVersion { get; set; } = null!;
}

public class CustomerCompanyContextDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public long CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public long? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public string RelationshipStatus { get; set; } = null!;
    public string? InternalNotes { get; set; }
    public DateTime? FirstInteractionAt { get; set; }
    public DateTime? LastInteractionAt { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCustomerCompanyContextRequest
{
    public long CompanyId { get; set; }
    public long? AssignedStaffId { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime? FirstInteractionAt { get; set; }
}

public class UpdateCustomerCompanyContextRequest
{
    public long? AssignedStaffId { get; set; }
    public string RelationshipStatus { get; set; } = null!;
    public string? InternalNotes { get; set; }
    public DateTime? LastInteractionAt { get; set; }
    public string TargetVersion { get; set; } = null!;
}

public class DuplicateCheckRequest
{
    public string? Cccd { get; set; }
    public string? Phone { get; set; }
}

public class DuplicateCheckResult
{
    public bool HasDuplicates { get; set; }
    public CustomerListItemDto[] Matches { get; set; } = Array.Empty<CustomerListItemDto>();
}

public class CustomerSearchRequest
{
    public string? Search { get; set; }
    public string? CustomerStatus { get; set; }
    public long? CompanyId { get; set; }
    public long? AssignedStaffId { get; set; }
    public bool? UnassignedStaff { get; set; }
    public long[]? TagIds { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CompanyLookupDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
}

public class StaffLookupDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = null!;
}

public class PagedResult<T>
{
    public T[] Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
