using System;

namespace PTKD.Application.CarePackages.DTOs;

public class CreateCarePackageRequest
{
    public long CustomerId { get; set; }

    /// <summary>Gói chăm sóc được chọn từ DANH MỤC dịch vụ (Service_Types có IsCarePackage = true).</summary>
    public long ServiceTypeId { get; set; }

    public DateTime SaleDate { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }

    // Only one item is generally created at a time in current UI, but domain supports many
    public CreateCarePackageRequestItem Item { get; set; } = null!;
}

public class CreateCarePackageRequestItem
{
    /// <summary>Phần mộ (bắt buộc) — số cốt được lấy TỰ ĐỘNG từ phần mộ, không nhận từ client.</summary>
    public long GraveId { get; set; }
    public DateTime ServicePeriodStartDate { get; set; }
}
