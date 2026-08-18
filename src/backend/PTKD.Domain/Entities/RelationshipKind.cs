namespace PTKD.Domain.Entities;

/// <summary>
/// Catalog loại quan hệ gia đình (trung tính giới tính). Nhãn tiếng Việt được
/// chọn theo giới tính đối tượng khi hiển thị. Bảng tham chiếu — chỉ đọc.
/// </summary>
public class RelationshipKind
{
    public const string Parent = "PARENT";
    public const string Child = "CHILD";
    public const string Spouse = "SPOUSE";
    public const string Sibling = "SIBLING";
    public const string SiblingOlder = "SIBLING_OLDER";
    public const string SiblingYounger = "SIBLING_YOUNGER";
    public const string GrandparentPaternal = "GRANDPARENT_PATERNAL";
    public const string GrandparentMaternal = "GRANDPARENT_MATERNAL";
    public const string GrandchildPaternal = "GRANDCHILD_PATERNAL";
    public const string GrandchildMaternal = "GRANDCHILD_MATERNAL";
    public const string Other = "OTHER";

    public string KindCode { get; private set; } = null!;
    public string LabelMale { get; private set; } = null!;
    public string LabelFemale { get; private set; } = null!;
    public string LabelNeutral { get; private set; } = null!;
    public string InverseCode { get; private set; } = null!;
    public bool IsSymmetric { get; private set; }
    public int SortOrder { get; private set; }

    private RelationshipKind() { }

    public RelationshipKind(
        string kindCode, string labelMale, string labelFemale, string labelNeutral,
        string inverseCode, bool isSymmetric, int sortOrder)
    {
        KindCode = Require(kindCode, nameof(kindCode));
        LabelMale = Require(labelMale, nameof(labelMale));
        LabelFemale = Require(labelFemale, nameof(labelFemale));
        LabelNeutral = Require(labelNeutral, nameof(labelNeutral));
        InverseCode = Require(inverseCode, nameof(inverseCode));
        IsSymmetric = isSymmetric;
        SortOrder = sortOrder;
    }

    /// <summary>Sửa nhãn + thứ tự. KHÔNG đổi mã/nghịch đảo (đổi cấu trúc thì xoá & tạo lại).</summary>
    public void Update(string labelMale, string labelFemale, string labelNeutral, int sortOrder)
    {
        LabelMale = Require(labelMale, nameof(labelMale));
        LabelFemale = Require(labelFemale, nameof(labelFemale));
        LabelNeutral = Require(labelNeutral, nameof(labelNeutral));
        SortOrder = sortOrder;
    }

    /// <summary>Gắn mã loại nghịch đảo (dùng khi tạo cặp bất đối xứng — nối 2 chiều).</summary>
    public void SetInverse(string inverseCode) => InverseCode = Require(inverseCode, nameof(inverseCode));

    private static string Require(string v, string name)
        => string.IsNullOrWhiteSpace(v) ? throw new System.ArgumentException($"{name} không được trống.") : v.Trim();

    /// <summary>Nhãn tiếng Việt theo giới tính đối tượng ('MALE'/'FEMALE'/khác).</summary>
    public string LabelFor(string? gender) => gender switch
    {
        "MALE" => LabelMale,
        "FEMALE" => LabelFemale,
        _ => LabelNeutral
    };
}
