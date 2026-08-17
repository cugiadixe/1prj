using System;
using PTKD.Domain.ValueObjects;

namespace PTKD.Domain.Entities;

public class Card
{
    public const string StatusActive = "ACTIVE";
    public const string StatusInactive = "INACTIVE";

    public long Id { get; private set; }
    public long CompanyId { get; private set; }
    public string? GraveId { get; private set; }
    public string? CardNumber { get; private set; }   // SỐ THẺ (vd 5180) — cấp lúc tạo thẻ
    public long? ServiceId { get; private set; }
    public int PrintCount { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long CreatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private Card() { }

    public static Card Create(long companyId, string? graveId, long? serviceId, long createdByUserId, string? cardNumber = null)
    {
        return new Card
        {
            CompanyId = companyId,
            GraveId = graveId,
            CardNumber = cardNumber,
            ServiceId = serviceId,
            PrintCount = 0,
            Status = StatusActive,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void IncrementPrintCount(long updatedByUserId)
    {
        PrintCount++;
        UpdatedAt = DateTime.UtcNow;
        // updatedByUserId giữ cho chữ ký API; Card chưa có cột updated_by nên chỉ cập nhật thời điểm.
    }
}
