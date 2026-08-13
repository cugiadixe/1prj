using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Common.Interfaces;

/// <summary>Lưu trữ file ảnh/tài liệu của phần mộ trên ổ đĩa (mỗi mộ 1 thư mục).</summary>
public interface IGraveFileStorage
{
    /// <summary>Lưu file vào thư mục của mộ; nếu là ảnh và generateThumbnail=true thì tạo thumbnail.</summary>
    Task<StoredFileResult> SaveAsync(long graveId, string storedName, Stream content, string contentType, bool generateThumbnail, CancellationToken ct = default);

    /// <summary>Mở stream đọc file gốc; null nếu không tồn tại.</summary>
    Stream? OpenRead(long graveId, string storedName);

    /// <summary>Mở stream đọc thumbnail; null nếu không có.</summary>
    Stream? OpenReadThumbnail(long graveId, string storedName);

    /// <summary>Xóa file gốc + thumbnail (nếu có).</summary>
    void Delete(long graveId, string storedName);
}

public sealed record StoredFileResult(long SizeBytes, bool HasThumbnail);
