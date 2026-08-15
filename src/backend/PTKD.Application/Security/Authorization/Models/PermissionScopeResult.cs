namespace PTKD.Application.Security.Authorization.Models;

/// <summary>
/// Kết quả kiểm quyền theo mô hình MỚI: không chỉ trả lời "có quyền hay không" mà còn
/// "có quyền Ở NHỮNG CÔNG TY NÀO".
///
/// Vì sao cần: mô hình cũ chỉ trả <c>bool</c>, nên nơi gọi không có cách nào biết phải lọc
/// dữ liệu theo công ty nào. Hệ quả là mọi endpoint hoặc chặn sạch, hoặc mở toang mọi công ty —
/// không có nấc giữa. Phạm vi nay là thuộc tính của LẦN CẤP (ô trong ma trận phân quyền),
/// không phải thuộc tính cứng của mã quyền.
/// </summary>
/// <param name="Granted">Người này có mã quyền đó ở ít nhất một phạm vi nào không.</param>
/// <param name="IsGlobal">
/// Có ít nhất một lần cấp ở phạm vi TOÀN CỤC — tức được nhìn dữ liệu của MỌI công ty.
/// Đây là "ngoại lệ có chủ ý" mà người quản trị phải chọn, không phải mặc định.
/// </param>
/// <param name="CompanyIds">Các công ty được cấp cụ thể (khi không toàn cục).</param>
/// <param name="DeniedCompanyIds">
/// Các công ty bị CẤM riêng. Tách ra khỏi <paramref name="CompanyIds"/> để lệnh cấm theo công ty
/// vẫn có tác dụng với người được cấp toàn cục — mô hình cũ không làm được việc này.
/// </param>
public sealed record PermissionScopeResult(
    bool Granted,
    bool IsGlobal,
    IReadOnlyList<long> CompanyIds,
    IReadOnlyList<long> DeniedCompanyIds)
{
    public static readonly PermissionScopeResult Denied =
        new(false, false, Array.Empty<long>(), Array.Empty<long>());

    /// <summary>Có được thao tác trên dữ liệu của công ty này không.</summary>
    public bool Allows(long companyId)
        => Granted
           && !DeniedCompanyIds.Contains(companyId)
           && (IsGlobal || CompanyIds.Contains(companyId));

    /// <summary>
    /// Được nhìn mọi công ty — nơi gọi KHÔNG gắn mệnh đề lọc công ty vào truy vấn.
    /// </summary>
    public bool IsUnrestricted => Granted && IsGlobal;

    /// <summary>
    /// Danh sách công ty được phép, dùng khi <see cref="IsUnrestricted"/> là false.
    /// Rỗng nghĩa là không thấy gì — KHÁC hẳn với "không lọc". Lẫn hai thứ này là mở toang dữ liệu.
    /// </summary>
    public IReadOnlyList<long> AllowedCompanyIds
        => !Granted
            ? Array.Empty<long>()
            : DeniedCompanyIds.Count == 0
                ? CompanyIds
                : CompanyIds.Where(id => !DeniedCompanyIds.Contains(id)).ToList();

    /// <summary>
    /// Các công ty phải loại trừ. Tách riêng vì trường hợp "cấp toàn cục nhưng cấm một công ty"
    /// không diễn đạt được bằng danh sách cho phép — nơi gọi phải áp thêm mệnh đề loại trừ này
    /// ngay cả khi <see cref="IsUnrestricted"/>.
    /// </summary>
    public IReadOnlyList<long> ExcludedCompanyIds => DeniedCompanyIds;
}

/// <summary>
/// Một dòng trong "bộ quyền hiệu dụng" của người dùng: mã quyền kèm phạm vi thật của nó.
///
/// Thay cho việc trả <c>data_scope</c> của DANH MỤC như trước — đó là nhãn tĩnh của mã quyền,
/// không phải phạm vi người dùng được cấp, nên giao diện không thể phân biệt người có quyền
/// toàn cục với người chỉ có quyền một công ty.
/// </summary>
public sealed record EffectivePermissionEntry(
    string PermissionCode,
    bool IsGlobal,
    IReadOnlyList<long> CompanyIds);
