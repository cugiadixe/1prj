using System;
using PTKD.Application.Tags.DTOs;

namespace PTKD.Application.Graves.DTOs;

/// <summary>Một dòng trong bảng "tổng hợp giấy tờ/tài liệu theo mộ".</summary>
public class GraveAttachmentSummaryDto
{
    public long GraveId { get; set; }
    public string GraveCode { get; set; } = null!;
    public string Zone { get; set; } = null!;
    public string GraveType { get; set; } = null!;
    public string? OwnerName { get; set; }
    public string? CemeteryName { get; set; }
    public int PhotoCount { get; set; }         // Ảnh
    public int TransferDocCount { get; set; }   // Văn bản chuyển nhượng
    public int OtherCount { get; set; }         // Khác
    public int TotalCount { get; set; }
    public DateTime? LastUploadedAt { get; set; }
}

public class GraveAttachmentSummaryRequest
{
    public string? Search { get; set; }             // theo mã mộ HOẶC tên chủ mộ
    public string? Zone { get; set; }               // khu A–L
    public string? Category { get; set; }           // PHOTO / TRANSFER_DOC / OTHER
    public long? UploadedByUserId { get; set; }     // người tải lên
    public DateTime? UploadedFrom { get; set; }     // khoảng ngày tải (từ)
    public DateTime? UploadedTo { get; set; }       // khoảng ngày tải (đến)
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Người từng tải tài liệu lên (để lọc "người upload").</summary>
public sealed record AttachmentUploaderDto(long UserId, string Name);

public class GraveListItemDto
{
    public long Id { get; set; }
    public string GraveCode { get; set; } = null!;
    public string Zone { get; set; } = null!;
    public string PlotNumber { get; set; } = null!;
    public string GraveType { get; set; } = null!;
    public decimal? AreaM2 { get; set; }
    public int CotCount { get; set; }
    public string Status { get; set; } = null!;
    public long? OwnerCustomerId { get; set; }
    public string? OwnerName { get; set; }
    public int OccupantCount { get; set; }
    public long? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
    public TagDto[] Tags { get; set; } = Array.Empty<TagDto>();
}

/// <summary>Công ty (qua nghĩa trang) để đổ vào bộ lọc danh sách mộ — chỉ công ty người gọi được phủ.</summary>
public class GraveCompanyLookupDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
}

public class GraveDetailDto
{
    public long Id { get; set; }
    public string GraveCode { get; set; } = null!;
    public string Zone { get; set; } = null!;
    public string PlotNumber { get; set; } = null!;
    public string? RowLabel { get; set; }
    public string? ColLabel { get; set; }
    public string GraveType { get; set; } = null!;
    public decimal? AreaM2 { get; set; }
    public int CotCount { get; set; }
    public string Status { get; set; } = null!;
    public long? OwnerCustomerId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerCode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? Notes { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public GraveOccupantDto[] Occupants { get; set; } = Array.Empty<GraveOccupantDto>();
    public GraveEmergencyContactDto[] EmergencyContacts { get; set; } = Array.Empty<GraveEmergencyContactDto>();
    public TagDto[] Tags { get; set; } = Array.Empty<TagDto>();
}

public class GraveEmergencyContactDto
{
    public long Id { get; set; }
    public long GraveId { get; set; }
    public int Priority { get; set; }
    public long? ContactCustomerId { get; set; }
    public string? ContactCode { get; set; }        // mã KH (nếu liên kết khách hàng)
    public string ContactName { get; set; } = null!; // tên KH hoặc tên nhập tay
    public string? ContactPhone { get; set; }        // SĐT theo hồ sơ KH (hoặc nhập tay)
    public string? RelationshipNote { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class CreateGraveEmergencyContactRequest
{
    public long ContactCustomerId { get; set; }
    public string? RelationshipNote { get; set; }
}

public class UpdateGraveEmergencyContactRequest
{
    public long ContactCustomerId { get; set; }
    public string? RelationshipNote { get; set; }
    public string TargetVersion { get; set; } = null!;
}

public class GraveOccupantDto
{
    public long Id { get; set; }
    public long GraveId { get; set; }
    public long? DeceasedCustomerId { get; set; }
    public string Status { get; set; } = null!;         // ACTIVE / RELOCATED
    public DateTime? RelocatedAt { get; set; }
    public string? RelocationNote { get; set; }
    public string FullName { get; set; } = null!;
    public string? Gender { get; set; }
    public DateTime? Dob { get; set; }
    public DateTime? DeathDateSolar { get; set; }
    public string? DeathDateLunar { get; set; }
    public DateTime? BurialDate { get; set; }
    public string? Hometown { get; set; }
    public string? OwnerRelationship { get; set; }
    public string? DeceasedRelationship { get; set; }
    public string? Notes { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class CreateGraveRequest
{
    public string GraveCode { get; set; } = null!;
    /// <summary>
    /// Nghĩa trang chứa mộ (mộ thuộc công ty qua nghĩa trang). Bỏ trống khi hệ chỉ có MỘT nghĩa
    /// trang thì hệ tự chọn; khi có nhiều nghĩa trang thì bắt buộc chọn.
    /// </summary>
    public long? CemeteryId { get; set; }
    public string Zone { get; set; } = null!;
    public string PlotNumber { get; set; } = null!;
    public string? RowLabel { get; set; }
    public string? ColLabel { get; set; }
    public string GraveType { get; set; } = null!;
    public decimal? AreaM2 { get; set; }
    public int CotCount { get; set; } = 1;
    public string Status { get; set; } = null!;
    public long? OwnerCustomerId { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? Notes { get; set; }
    public CreateGraveOccupantRequest[] Occupants { get; set; } = Array.Empty<CreateGraveOccupantRequest>();
}

public class UpdateGraveRequest
{
    public string Zone { get; set; } = null!;
    public string PlotNumber { get; set; } = null!;
    public string? RowLabel { get; set; }
    public string? ColLabel { get; set; }
    public string GraveType { get; set; } = null!;
    public decimal? AreaM2 { get; set; }
    public int CotCount { get; set; } = 1;
    public string Status { get; set; } = null!;
    public long? OwnerCustomerId { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? Notes { get; set; }
    public string TargetVersion { get; set; } = null!;
}

public class CreateGraveOccupantRequest
{
    public string FullName { get; set; } = null!;
    public string? Gender { get; set; }
    public DateTime? Dob { get; set; }
    public DateTime? DeathDateSolar { get; set; }
    public string? DeathDateLunar { get; set; }
    public DateTime? BurialDate { get; set; }
    public string? Hometown { get; set; }
    public string? OwnerRelationship { get; set; }
    public string? DeceasedRelationship { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Đặt một khách hàng ĐÃ MẤT vào cốt (luồng mới): occupant nối với bản ghi khách, thông tin cốt
/// chụp từ hồ sơ khách, nhãn quan hệ tự suy. Khác CreateGraveOccupantRequest (nhập tay, legacy).
/// </summary>
public class PlaceGraveOccupantRequest
{
    public long DeceasedCustomerId { get; set; }
    public DateTime? BurialDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Bốc/cải táng một suất: chuyển RELOCATED, giải phóng người + chỗ.</summary>
public class RelocateOccupantRequest
{
    public DateTime? RelocatedAt { get; set; }
    public string? Note { get; set; }
}

/// <summary>Phần mộ có thể gán chủ cho một khách: TRỐNG + CHƯA có chủ + thuộc công ty của khách.</summary>
public class AssignableGraveDto
{
    public long GraveId { get; set; }
    public string GraveCode { get; set; } = null!;
    public string Zone { get; set; } = null!;
    public string RowVersion { get; set; } = null!;   // để gọi chuyển-quyền (kiểm tương tranh)
}

/// <summary>Khách hàng đủ điều kiện đặt vào cốt của một mộ (đã mất + có quan hệ với chủ + chưa nằm mộ).</summary>
public class OccupantCandidateDto
{
    public long CustomerId { get; set; }
    public string CustomerCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string RelationLabel { get; set; } = null!; // cốt LÀ gì của chủ mộ (đã suy theo giới tính)
}

public class UpdateGraveOccupantRequest
{
    public string FullName { get; set; } = null!;
    public string? Gender { get; set; }
    public DateTime? Dob { get; set; }
    public DateTime? DeathDateSolar { get; set; }
    public string? DeathDateLunar { get; set; }
    public DateTime? BurialDate { get; set; }
    public string? Hometown { get; set; }
    public string? OwnerRelationship { get; set; }
    public string? DeceasedRelationship { get; set; }
    public string? Notes { get; set; }
    public string TargetVersion { get; set; } = null!;
}

public class GraveSearchRequest
{
    public string? Search { get; set; }
    public string? Zone { get; set; }
    public string? Status { get; set; }
    public string? GraveType { get; set; }
    public long? OwnerCustomerId { get; set; }
    public long? CompanyId { get; set; }
    /// <summary>So sánh SỐ NGƯỜI AN TÁNG (cốt ACTIVE) với SỐ CỐT: UNDER = còn chỗ (&lt;), FULL = đã đủ (=), OVER = vượt số cốt (&gt;).</summary>
    public string? Capacity { get; set; }
    public long[]? TagIds { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResult<T>
{
    public T[] Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
