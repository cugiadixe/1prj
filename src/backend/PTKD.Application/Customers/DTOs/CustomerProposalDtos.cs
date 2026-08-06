using System;

namespace PTKD.Application.Customers.DTOs;

public class CreateCustomerProposalRequest
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

public class CustomerProposalDto
{
    public long Id { get; set; }
    public string ProcessCode { get; set; } = null!;
    public long RequesterId { get; set; }
    public long? CompanyId { get; set; }
    public string RequestStatus { get; set; } = null!;
    public long? WorkflowInstanceId { get; set; }
    public long? CreatedCustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = null!;
    public CustomerProposalSummaryDto? Summary { get; set; }
}

public class CustomerProposalSummaryDto
{
    public string CustomerCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public long? CompanyId { get; set; }
}
