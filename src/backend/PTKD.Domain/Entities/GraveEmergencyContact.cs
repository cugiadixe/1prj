using System;

namespace PTKD.Domain.Entities;

/// <summary>
/// Liên hệ khẩn cấp ĐỘNG của phần mộ: nhiều người, có thứ tự ưu tiên (1 = gọi trước tiên).
/// Người liên hệ LÀ khách hàng (contact_customer_id) — tên/SĐT tự theo hồ sơ KH.
/// Vẫn giữ contact_name/phone nhập tay ở tầng schema để tương thích, nhưng luồng hiện tại dùng khách hàng.
/// </summary>
public class GraveEmergencyContact
{
    public long Id { get; private set; }
    public long GraveId { get; private set; }
    public int Priority { get; private set; }                 // 1 = gọi trước tiên
    public long? ContactCustomerId { get; private set; }      // liên kết KH (động) — SĐT tự theo hồ sơ
    public string? ContactName { get; private set; }          // hoặc nhập tay (không dùng ở luồng hiện tại)
    public string? ContactPhone { get; private set; }
    public string? RelationshipNote { get; private set; }     // quan hệ với chủ mộ / ghi chú
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    public Customer? Contact { get; private set; }

    private GraveEmergencyContact() { }

    public GraveEmergencyContact(
        long graveId, int priority, long? contactCustomerId,
        string? contactName, string? contactPhone, string? relationshipNote, long? createdByUserId)
    {
        GraveId = graveId;
        Priority = priority < 1 ? 1 : priority;
        ContactCustomerId = contactCustomerId;
        ContactName = contactName;
        ContactPhone = contactPhone;
        RelationshipNote = relationshipNote;
        IsActive = true;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(long? contactCustomerId, string? contactName, string? contactPhone, string? relationshipNote, long? updatedByUserId)
    {
        ContactCustomerId = contactCustomerId;
        ContactName = contactName;
        ContactPhone = contactPhone;
        RelationshipNote = relationshipNote;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    public void SetPriority(int priority, long? updatedByUserId)
    {
        Priority = priority < 1 ? 1 : priority;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
