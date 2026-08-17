using System;

namespace PTKD.Domain.Entities;

/// <summary>Ảnh/tài liệu đính kèm phần mộ. File nằm trên ổ đĩa server; entity giữ metadata.</summary>
public class GraveAttachment
{
    public const string CategoryPhoto = "PHOTO";
    public const string CategoryTransferDoc = "TRANSFER_DOC";
    public const string CategoryOther = "OTHER";

    public long Id { get; private set; }
    public long GraveId { get; private set; }
    public string Category { get; private set; } = null!;
    public long? OwnershipHistoryId { get; private set; }
    public string FileNameOriginal { get; private set; } = null!;
    public string StoredName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public bool HasThumbnail { get; private set; }
    public string? Description { get; private set; }
    /// <summary>Đường dẫn gốc lưu trữ lúc file được ghi. NULL = file cũ, nằm ở gốc mặc định
    /// (appsettings). Nhờ vậy đổi đường dẫn hiện tại KHÔNG làm hỏng file cũ.</summary>
    public string? StorageBasePath { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private GraveAttachment() { }

    public GraveAttachment(
        long graveId, string category, long? ownershipHistoryId,
        string fileNameOriginal, string storedName, string contentType,
        long sizeBytes, bool hasThumbnail, string? description, long? createdByUserId,
        string? storageBasePath = null)
    {
        GraveId = graveId;
        Category = category ?? throw new ArgumentNullException(nameof(category));
        OwnershipHistoryId = ownershipHistoryId;
        FileNameOriginal = fileNameOriginal ?? throw new ArgumentNullException(nameof(fileNameOriginal));
        StoredName = storedName ?? throw new ArgumentNullException(nameof(storedName));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        SizeBytes = sizeBytes;
        HasThumbnail = hasThumbnail;
        Description = description;
        StorageBasePath = storageBasePath;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description, long? updatedByUserId)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
