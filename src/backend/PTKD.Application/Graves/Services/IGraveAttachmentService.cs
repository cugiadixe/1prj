using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Graves.DTOs;

namespace PTKD.Application.Graves.Services;

public interface IGraveAttachmentService
{
    Task<GraveAttachmentDto> UploadAsync(
        long graveId, string category, long? ownershipHistoryId,
        string fileName, string contentType, long size, Stream content,
        string? description, long actorUserId, CancellationToken ct = default);

    Task<IReadOnlyList<GraveAttachmentDto>> ListAsync(long graveId, long actorUserId, CancellationToken ct = default);

    /// <summary>
    /// Trả metadata + stream để controller phát file (gốc hoặc thumbnail). Null nếu không có.
    /// <paramref name="graveId"/> BẮT BUỘC: đính kèm phải thuộc đúng mộ đó, và người gọi phải xem
    /// được mộ đó (theo công ty). Trước đây bỏ qua graveId nên tải chéo được đính kèm mộ khác.
    /// </summary>
    Task<AttachmentContent?> OpenContentAsync(long graveId, long attachmentId, long actorUserId, bool thumbnail, CancellationToken ct = default);

    Task DeleteAsync(long graveId, long attachmentId, long actorUserId, CancellationToken ct = default);
}

public sealed record AttachmentContent(Stream Stream, string ContentType, string FileName);
