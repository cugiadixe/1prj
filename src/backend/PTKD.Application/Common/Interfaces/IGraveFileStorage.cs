using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Common.Interfaces;

/// <summary>
/// Lưu trữ file ảnh/tài liệu của phần mộ trên ổ đĩa. Mỗi mộ một thư mục, đặt tên theo MÃ MỘ
/// (<c>graveKey</c> = grave_code, vd "A-0001").
///
/// Đường dẫn GỐC cấu hình runtime được (App_Settings). Để đổi gốc KHÔNG làm hỏng file cũ, mỗi lần
/// đọc/xoá truyền vào <c>basePath</c> = gốc đã lưu của chính file đó (NULL = file cũ → dùng gốc
/// mặc định appsettings). Lưu mới luôn dùng gốc HIỆN TẠI và trả về trong <see cref="StoredFileResult.BasePathUsed"/>.
/// </summary>
public interface IGraveFileStorage
{
    /// <summary>Lưu file vào gốc HIỆN TẠI; trả về kích thước, có thumbnail, và gốc đã dùng.</summary>
    Task<StoredFileResult> SaveAsync(string graveKey, string storedName, Stream content, string contentType, bool generateThumbnail, CancellationToken ct = default);

    /// <summary>Mở stream đọc file gốc; null nếu không tồn tại. basePath = gốc đã lưu (null → mặc định).</summary>
    Stream? OpenRead(string? basePath, string graveKey, string storedName);

    /// <summary>Mở stream đọc thumbnail; null nếu không có.</summary>
    Stream? OpenReadThumbnail(string? basePath, string graveKey, string storedName);

    /// <summary>Xóa file gốc + thumbnail (nếu có).</summary>
    void Delete(string? basePath, string graveKey, string storedName);
}

public sealed record StoredFileResult(long SizeBytes, bool HasThumbnail, string BasePathUsed);
