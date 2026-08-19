namespace PTKD.Application.Relationships.DTOs;

/// <summary>Một loại quan hệ cho người dùng chọn khi khai (nhãn trung tính giới tính).</summary>
public class RelationshipKindDto
{
    public string KindCode { get; set; } = null!;
    public string Label { get; set; } = null!;        // nhãn trung tính để hiển thị trong dropdown
    public string InverseCode { get; set; } = null!;
    public bool IsSymmetric { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Một cạnh quan hệ nhìn từ khách hàng đang xem: "người kia LÀ &lt;RelationLabel&gt; của khách này".
/// </summary>
public class CustomerRelationshipDto
{
    public long Id { get; set; }
    public long FromCustomerId { get; set; }          // khách đang xem
    public long OtherCustomerId { get; set; }         // người thân (to)
    public string OtherCustomerCode { get; set; } = null!;
    public string OtherCustomerName { get; set; } = null!;
    public string RelationKind { get; set; } = null!;
    public string RelationLabel { get; set; } = null!; // nhãn đã giải theo giới tính người thân
    public bool IsDerived { get; set; }
    public bool NeedsConfirmation { get; set; }
    public string? Note { get; set; }
    public string RowVersion { get; set; } = null!;

    // ─── Đan chéo 360: dấu vết phần mộ + tình trạng của NGƯỜI THÂN ───
    // Tình trạng sống/mất (từ CustomerStatus của người thân — không cần quyền mộ).
    public bool IsDeceased { get; set; }
    // Phần mộ người thân đang SỞ HỮU và nơi người thân ĐƯỢC AN TÁNG (suất còn hiệu lực). CHỈ điền khi
    // người gọi có GRAVE_VIEW; rỗng/null nếu không đủ quyền mộ (không lộ mộ ngoài phạm vi).
    public GraveRefDto[] OwnedGraves { get; set; } = System.Array.Empty<GraveRefDto>();
    public GraveRefDto? BuriedIn { get; set; }
}

/// <summary>Tham chiếu gọn tới một phần mộ (id + mã) để hiển thị link.</summary>
public class GraveRefDto
{
    public long GraveId { get; set; }
    public string GraveCode { get; set; } = null!;
}

public class CreateCustomerRelationshipRequest
{
    public long OtherCustomerId { get; set; }         // người thân
    public string RelationKind { get; set; } = null!; // "người thân LÀ <kind> của khách đang xem"
    public string? Note { get; set; }
}

/// <summary>Một quan hệ trong danh sách quản lý toàn hệ: "B LÀ &lt;RelationLabel&gt; của A".</summary>
public class RelationshipListItemDto
{
    public long Id { get; set; }
    public long FromCustomerId { get; set; }          // A (góc nhìn)
    public string FromCustomerCode { get; set; } = null!;
    public string FromCustomerName { get; set; } = null!;
    public long ToCustomerId { get; set; }            // B (đối tượng)
    public string ToCustomerCode { get; set; } = null!;
    public string ToCustomerName { get; set; } = null!;
    public string RelationKind { get; set; } = null!;
    public string RelationLabel { get; set; } = null!; // nhãn đã giải theo giới tính B
    public bool IsDerived { get; set; }
    public bool NeedsConfirmation { get; set; }
    public string? Note { get; set; }
}

public class RelationshipSearchRequest
{
    public string? Search { get; set; }               // khớp tên/mã của một trong hai đầu
    public string? Kind { get; set; }                 // loại quan hệ (khớp cả chiều nghịch đảo)
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class RelationshipPagedResult<T>
{
    public T[] Items { get; set; } = System.Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// ─── Quản lý DANH MỤC loại quan hệ (Relationship_Kinds) ──────────────────────

public class RelationshipKindDetailDto
{
    public string KindCode { get; set; } = null!;
    public string LabelMale { get; set; } = null!;
    public string LabelFemale { get; set; } = null!;
    public string LabelNeutral { get; set; } = null!;
    public string InverseCode { get; set; } = null!;
    public string? InverseLabelNeutral { get; set; }   // nhãn chung của loại nghịch đảo (để hiển thị cặp)
    public bool IsSymmetric { get; set; }
    public int SortOrder { get; set; }
    public bool IsCore { get; set; }                    // loại hệ thống — không cho xoá
    public bool Deletable { get; set; }                 // false nếu core hoặc đang bị tham chiếu
}

public class RelationshipKindSideInput
{
    public string LabelMale { get; set; } = null!;
    public string LabelFemale { get; set; } = null!;
    public string LabelNeutral { get; set; } = null!;
}

public class CreateRelationshipKindRequest
{
    public bool IsSymmetric { get; set; }               // true: Vợ/Chồng, Anh-Chị-Em... (1 loại tự nghịch đảo)
    public RelationshipKindSideInput SideA { get; set; } = null!;
    public RelationshipKindSideInput? SideB { get; set; } // bắt buộc khi bất đối xứng (vd Mẹ kế ↔ Con riêng)
    public int SortOrder { get; set; }
}

public class UpdateRelationshipKindRequest
{
    public string LabelMale { get; set; } = null!;
    public string LabelFemale { get; set; } = null!;
    public string LabelNeutral { get; set; } = null!;
    public int SortOrder { get; set; }
}
