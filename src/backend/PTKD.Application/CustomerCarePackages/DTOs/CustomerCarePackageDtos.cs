using System;

namespace PTKD.Application.CustomerCarePackages.DTOs;

public class CustomerCarePackageDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long ServiceTypeId { get; set; }
    public string? ServiceTypeName { get; set; }
    public int? CycleDurationMonths { get; set; }
    public long? GraveId { get; set; }
    public string? GraveCode { get; set; }
    public int? GraveCotCount { get; set; }
    public int CotCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCustomerCarePackageRequest
{
    public long CustomerId { get; set; }
    public long ServiceTypeId { get; set; }
    public int CotCount { get; set; } = 1;
    public DateTime StartDate { get; set; }
    public string? Notes { get; set; }
}

public class AssignGraveRequest
{
    public long GraveId { get; set; }
}
