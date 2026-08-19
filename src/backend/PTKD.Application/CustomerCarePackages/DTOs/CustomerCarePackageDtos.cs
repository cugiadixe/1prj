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
    // Cách tính giá của loại dịch vụ: PER_COT (× số cốt) hoặc PER_GRAVE (theo phần mộ, × 1).
    public string? PricingBasis { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public long? WorkflowInstanceId { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
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
