using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Common.Interfaces;

/// <summary>
/// Lưu trữ file ảnh/tài liệu của phần mộ trên ổ đĩa. Mỗi mộ một thư mục, đặt tên theo MÃ MỘ
/// (<c>graveKey</c> = grave_code, vd "A-0001") cho dễ tra tay trên server.
/// </summary>
public interface IGraveFileStorage
{
    /// <summary>Lưu file vào thư mục của mộ; nếu là ảnh và generateThumbnail=true thì tạo thumbnail.</summary>
    Task<StoredFileResult> SaveAsync(string graveKey, string storedName, Stream content, string contentType, bool generateThumbnail, CancellationToken ct = default);

    /// <summary>Mở stream đọc file gốc; null nếu không tồn tại.</summary>
    Stream? OpenRead(string graveKey, string storedName);

    /// <summary>Mở stream đọc thumbnail; null nếu không có.</summary>
    Stream? OpenReadThumbnail(string graveKey, string storedName);

    /// <summary>Xóa file gốc + thumbnail (nếu có).</summary>
    void Delete(string graveKey, string storedName);
}

public sealed record StoredFileResult(long SizeBytes, bool HasThumbnail);
