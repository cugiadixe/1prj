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
    // Suy từ CustomerStatus == "DECEASED": khách đã trở thành cốt trong mộ (đã mất). Tách ra thành
    // cờ riêng cho cột "Tình trạng" (Còn sống / Đã mất) ở danh sách.
    public bool IsDeceased { get; set; }
    // Các công ty phụ trách khách này (kèm nhân viên phụ trách). ĐÃ được lọc theo phạm vi quyền của
    // người gọi — không liệt kê công ty người gọi không được phủ.
    public CustomerCompanyBriefDto[] Companies { get; set; } = Array.Empty<CustomerCompanyBriefDto>();
    // Phần mộ khách này đang SỞ HỮU (chủ mộ). ĐÃ lọc theo phạm vi GRAVE_VIEW của người gọi.
    public OwnedGraveDto[] OwnedGraves { get; set; } = Array.Empty<OwnedGraveDto>();
    public DateTime CreatedAt { get; set; }
    public TagDto[] Tags { get; set; } = Array.Empty<TagDto>();
}

/// <summary>Một phần mộ khách đang sở hữu (dùng cho cột danh sách khách).</summary>
public class OwnedGraveDto
{
    public long GraveId { get; set; }
    public string GraveCode { get; set; } = null!;
}

// ─── Bảng điều khiển 360 của khách (mộ sở hữu + mộ được an táng) ─────────────────
// Dữ liệu MỘ theo phạm vi GRAVE_VIEW riêng, KHÔNG dùng chung scope khách. Nếu người gọi không có
// GRAVE_VIEW thì trả rỗng + GraveAccessDenied=true để FE báo "không đủ quyền" thay vì "không có mộ".
public class CustomerOverviewDto
{
    public OverviewGraveDto[] OwnedGraves { get; set; } = Array.Empty<OverviewGraveDto>();
    public BuriedInGraveDto[] BuriedIn { get; set; } = Array.Empty<BuriedInGraveDto>();
    public bool GraveAccessDenied { get; set; }
}

/// <summary>Một phần mộ khách đang SỞ HỮU (chủ mộ) — kèm nghĩa trang + số cốt đang an táng/sức chứa.</summary>
public class OverviewGraveDto
{
    public long GraveId { get; set; }
    public string GraveCode { get; set; } = null!;
    public string? CemeteryName { get; set; }
    public string Zone { get; set; } = null!;
    public string PlotNumber { get; set; } = null!;
    public string GraveType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int CotCount { get; set; }              // sức chứa thiết kế
    public int ActiveOccupantCount { get; set; }   // số cốt đang an táng (suất ACTIVE)
}

/// <summary>Một phần mộ khách ĐƯỢC AN TÁNG (là cốt) — qua GraveOccupant.DeceasedCustomerId.</summary>
public class BuriedInGraveDto
{
    public long GraveId { get; set; }
    public string GraveCode { get; set; } = null!;
    public string? CemeteryName { get; set; }
    public string Zone { get; set; } = null!;
    public string GraveStatus { get; set; } = null!;      // trạng thái phần mộ
    public string OccupantStatus { get; set; } = null!;   // suất của khách: ACTIVE / RELOCATED
    public DateTime? BurialDate { get; set; }
    public DateTime? RelocatedAt { get; set; }
    public string? DeceasedRelationship { get; set; }     // khách (người mất) → chủ mộ
    public long? OwnerCustomerId { get; set; }
    public string? OwnerName { get; set; }
}

/// <summary>Tóm tắt công ty phụ trách + nhân viên phụ trách của khách, dùng cho cột danh sách.</summary>
public class CustomerCompanyBriefDto
{
    public long CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public long? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
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

    // Khách được tạo trong tình trạng ĐÃ MẤT → đặt CustomerStatus = DECEASED ngay lúc tạo (khách đã
    // mất nhưng chưa gắn mộ). Mặc định false (còn sống). Xem CustomerService.CreateCustomerAsync.
    public bool IsDeceased { get; set; }

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
    // Lọc theo tình trạng sống/mất: "ALIVE" (CustomerStatus != DECEASED) hoặc "DECEASED".
    // Độc lập với CustomerStatus ở trên; cả hai đều đọc cùng cột nên chọn mâu thuẫn sẽ ra rỗng.
    public string? LifeStatus { get; set; }
    public long? CompanyId { get; set; }
    public long? AssignedStaffId { get; set; }
    public bool? UnassignedStaff { get; set; }
    public long[]? TagIds { get; set; }
    // Lọc theo sở hữu phần mộ: true = đang sở hữu ≥1 mộ (trong phạm vi), false = không sở hữu mộ nào.
    public bool? OwnsGrave { get; set; }
    // Lọc "chưa có phần mộ": true = khách CHƯA là cốt đang an táng ở mộ nào (dùng cho ô chọn người
    // thân đã mất khi khai quan hệ, để lọc ra người cần đặt cốt).
    public bool? NotBuried { get; set; }
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
