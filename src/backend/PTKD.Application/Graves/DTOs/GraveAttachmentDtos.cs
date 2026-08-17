using System;

namespace PTKD.Application.Graves.DTOs;

public class GraveAttachmentDto
{
    public long Id { get; set; }
    public long GraveId { get; set; }
    public string Category { get; set; } = null!;
    public long? OwnershipHistoryId { get; set; }
    public string FileNameOriginal { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public bool HasThumbnail { get; set; }
    public bool IsImage { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public string? UploadedByName { get; set; }
}
