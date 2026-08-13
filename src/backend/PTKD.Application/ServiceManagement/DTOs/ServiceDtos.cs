using System;

namespace PTKD.Application.ServiceManagement.DTOs;

public class ServiceTypeDto
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal StandardPrice { get; set; }
    public string StandardPriceCurrency { get; set; } = null!;
    public int? CycleDurationMonths { get; set; }
    public bool IsCarePackage { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class CreateServiceTypeRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal StandardPrice { get; set; }
    public int? CycleDurationMonths { get; set; }
    public bool IsCarePackage { get; set; }
}

public class UpdateServiceTypeRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? CycleDurationMonths { get; set; }
    public bool IsCarePackage { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class ServiceDto
{
    public long Id { get; set; }
    public long ServiceTypeId { get; set; }
    public string? ServiceTypeCode { get; set; }
    public string? ServiceTypeName { get; set; }
    public long CustomerId { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public long CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string Status { get; set; } = null!;
    public decimal AppliedPrice { get; set; }
    public decimal StandardPriceSnapshot { get; set; }
    public bool IsOverridePrice { get; set; }
    public long? OverrideApprovalRequestId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int CycleNumber { get; set; }
    public long? PreviousServiceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class CreateServiceRequest
{
    public long ServiceTypeId { get; set; }
    public long CustomerId { get; set; }
    public long CompanyId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class RenewServiceRequest
{
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class RequestPriceOverrideRequest
{
    public decimal RequestedPrice { get; set; }
    public string Reason { get; set; } = null!;
    public string RowVersion { get; set; } = null!;
}
