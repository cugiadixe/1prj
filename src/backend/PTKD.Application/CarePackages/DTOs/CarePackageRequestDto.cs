using System;
using System.Collections.Generic;

namespace PTKD.Application.CarePackages.DTOs;

public class CarePackageRequestDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerCode { get; set; }
    public string? ServiceName { get; set; }
    public string Status { get; set; } = null!;
    public bool RequiresApproval { get; set; }
    public long? WorkflowInstanceId { get; set; }
    public long? ServiceId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }
    public decimal TotalAmount { get; set; }
    public long? PaymentTransactionId { get; set; }
    public long? PreviousRequestId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    
    public List<CarePackageRequestItemDto> Items { get; set; } = new();
}

public class CarePackageRequestItemDto
{
    public long Id { get; set; }
    public long CarePackageRequestId { get; set; }
    public string? GraveId { get; set; }
    public int CotCountSnapshot { get; set; }
    public DateTime ServicePeriodStartDate { get; set; }
    public DateTime ServicePeriodEndDate { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal LineSubtotal { get; set; }
    public string? Notes { get; set; }
}
