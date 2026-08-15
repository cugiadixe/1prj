namespace PTKD.Application.Security.Authorization.Interfaces;

/// <summary>
/// Nở một tập công ty xuống theo cây cha–con.
///
/// Vì sao cần: anh Bách chốt mô hình "cấp ở công ty MẸ thì phủ mọi công ty CON" — nhân viên
/// tập đoàn (gán ở công ty mẹ) xem/thao tác được dữ liệu của các công ty con, còn nhân viên một
/// công ty con thì chỉ thấy công ty mình. Trước đây engine chỉ có hai nấc phẳng: một công ty
/// (COMPANY) hoặc mọi công ty (GLOBAL), không có tầng ở giữa "cả một nhánh của tập đoàn".
///
/// Đặt ở đây làm NGUỒN DUY NHẤT để cả bộ đánh giá quyền (nở công ty được cấp) lẫn
/// <see cref="ICompanyContextService"/> (nở công ty thành viên) dùng chung một phép nở — tránh
/// lặp lại lỗi cũ "sáu cách trả lời khác nhau cho cùng một câu hỏi".
/// </summary>
public interface ICompanyHierarchyService
{
    /// <summary>
    /// Với mỗi công ty trong <paramref name="companyIds"/>, trả về CHÍNH nó cùng toàn bộ công ty
    /// con (đệ quy, mọi cấp). Tập vào rỗng → trả rỗng. Kết quả không trùng lặp.
    /// </summary>
    Task<IReadOnlyCollection<long>> ExpandWithDescendantsAsync(
        IEnumerable<long> companyIds,
        CancellationToken cancellationToken = default);
}
