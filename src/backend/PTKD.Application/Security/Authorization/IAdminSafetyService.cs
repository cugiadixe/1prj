namespace PTKD.Application.Security.Authorization;

/// <summary>
/// Chốt an toàn chống "khoá chết hệ": không để mất người quản trị cuối cùng.
///
/// Vì sao: đăng nhập đòi tài khoản + việc làm ACTIVE. Nếu vô hiệu/khoá/ngừng người duy nhất còn
/// giữ một quyền quản trị sống còn (vd SECURITY_ADMIN_MANAGE) thì KHÔNG CÒN AI vào sửa lại được.
/// </summary>
public interface IAdminSafetyService
{
    /// <summary>
    /// <paramref name="userId"/> có phải là người ĐANG HOẠT ĐỘNG DUY NHẤT còn giữ
    /// <paramref name="permissionCode"/> không (vô hiệu người này = mất sạch quyền đó khỏi hệ).
    /// </summary>
    Task<bool> IsLastActiveHolderAsync(long userId, string permissionCode, CancellationToken cancellationToken = default);
}
