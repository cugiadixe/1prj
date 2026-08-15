namespace PTKD.Application.Security.Authorization.Models;

public enum PermissionScope
{
    /// <summary>
    /// Endpoint không mang ngữ cảnh công ty; cổng đòi lần cấp TOÀN CỤC. Dùng cho thao tác thật sự
    /// xuyên công ty (quản trị hệ thống), không lọc dữ liệu theo công ty.
    /// </summary>
    Global,

    /// <summary>
    /// Endpoint mang ngữ cảnh công ty qua header <c>X-Company-Id</c>; cổng kiểm người gọi THUỘC
    /// công ty đó và quyền PHỦ được công ty đó.
    /// </summary>
    Company,

    /// <summary>
    /// Cổng CHỈ kiểm "người gọi có mã quyền này ở BẤT KỲ phạm vi nào" (không đòi toàn cục, không
    /// đòi header). Phạm vi công ty do TẦNG SERVICE tự lọc theo dữ liệu — vì công ty của bản ghi
    /// suy ra từ CHÍNH dữ liệu (vd mộ → nghĩa trang → công ty), không phải do client khai.
    ///
    /// ⚠️ FOOTGUN: dùng ServiceFiltered mà service KHÔNG lọc theo công ty = lộ dữ liệu chéo công ty.
    /// Mọi endpoint ServiceFiltered PHẢI lọc qua các helper *CompanyScope (vd GraveCompanyScope).
    /// </summary>
    ServiceFiltered
}
