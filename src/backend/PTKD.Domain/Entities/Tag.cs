using System;

namespace PTKD.Domain.Entities;

/// <summary>
/// Thẻ (hashtag) dùng chung theo từ điển, TÁCH RIÊNG theo loại đối tượng (CUSTOMER / GRAVE).
/// Tên duy nhất trong phạm vi một loại. Có màu để hiển thị.
/// </summary>
public class Tag
{
    public const string TypeCustomer = "CUSTOMER";
    public const string TypeGrave = "GRAVE";

    public long Id { get; private set; }
    public string TagType { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Color { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private Tag() { }

    public Tag(string tagType, string name, string? color, long? createdByUserId)
    {
        TagType = tagType ?? throw new ArgumentNullException(nameof(tagType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Color = color;
        IsActive = true;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? color, bool isActive, long? updatedByUserId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Color = color;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
