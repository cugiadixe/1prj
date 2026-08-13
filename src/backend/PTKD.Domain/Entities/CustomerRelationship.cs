using System;

namespace PTKD.Domain.Entities;

/// <summary>
/// Cạnh quan hệ gia đình có hướng giữa 2 khách hàng: "To là &lt;RelationKind&gt;
/// của From". Lưu 2 chiều (cạnh nghịch đảo là một bản ghi riêng).
/// </summary>
public class CustomerRelationship
{
    public long Id { get; private set; }
    public long FromCustomerId { get; private set; }
    public long ToCustomerId { get; private set; }
    public string RelationKind { get; private set; } = null!;
    public bool IsDerived { get; private set; }
    public bool NeedsConfirmation { get; private set; }
    public string? Note { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private CustomerRelationship() { }

    public CustomerRelationship(
        long fromCustomerId, long toCustomerId, string relationKind,
        bool isDerived, bool needsConfirmation, string? note, long? createdByUserId)
    {
        if (fromCustomerId == toCustomerId)
            throw new ArgumentException("Quan hệ không thể trỏ về chính mình.");
        FromCustomerId = fromCustomerId;
        ToCustomerId = toCustomerId;
        RelationKind = relationKind ?? throw new ArgumentNullException(nameof(relationKind));
        IsDerived = isDerived;
        NeedsConfirmation = needsConfirmation;
        Note = note;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Reclassify(string relationKind, bool isDerived, bool needsConfirmation, long? updatedByUserId)
    {
        RelationKind = relationKind ?? throw new ArgumentNullException(nameof(relationKind));
        IsDerived = isDerived;
        NeedsConfirmation = needsConfirmation;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
