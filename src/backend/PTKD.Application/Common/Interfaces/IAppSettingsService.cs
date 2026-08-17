using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Common.Interfaces;

/// <summary>Đọc/ghi cấu hình hệ thống runtime (bảng App_Settings), có cache.</summary>
public interface IAppSettingsService
{
    /// <summary>Khoá cấu hình đường dẫn gốc lưu file đính kèm mộ.</summary>
    public const string FileStorageBasePathKey = "FileStorage:BasePath";

    /// <summary>Lấy giá trị (đồng bộ, có cache) — null nếu chưa cấu hình. Dùng ở tầng lưu file.</summary>
    string? GetValue(string key);

    /// <summary>Lấy giá trị (bất đồng bộ) — để hiển thị.</summary>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Đặt/xoá giá trị (value rỗng = về mặc định) và làm mới cache.</summary>
    Task SetValueAsync(string key, string? value, long actingUserId, CancellationToken cancellationToken = default);
}
