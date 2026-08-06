using System;

namespace PTKD.Application.CarePackages.DTOs;

public class CreateCarePackageRequest
{
    public long CustomerId { get; set; }
    public long? ServiceId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }
    
    // Only one item is generally created at a time in current UI, but domain supports many
    public CreateCarePackageRequestItem Item { get; set; } = null!;
}

public class CreateCarePackageRequestItem
{
    public string? GraveId { get; set; }
    public int CotCount { get; set; }
    public DateTime ServicePeriodStartDate { get; set; }
}
