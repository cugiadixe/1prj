using System;

namespace PTKD.Domain.Entities;

/// <summary>Lịch sử chuyển quyền sở hữu phần mộ (bán / thừa kế / qua đời / đính chính).</summary>
public class GraveOwnershipHistory
{
    public const string TypeSale = "SALE";                // sang nhượng / bán
    public const string TypeGift = "GIFT";                // cho / tặng
    public const string TypeRelocation = "RELOCATION";    // chuyển công tác / chuyển nơi ở
    public const string TypeInheritance = "INHERITANCE";  // thừa kế
    public const string TypeDeath = "DEATH";              // chủ mộ qua đời
    public const string TypeCorrection = "CORRECTION";    // đính chính

    public long Id { get; private set; }
    public long GraveId { get; private set; }
    public long? PreviousOwnerId { get; private set; }
    public long NewOwnerId { get; private set; }
    public string TransferType { get; private set; } = null!;
    public string? Reason { get; private set; }
    public DateTime TransferredAt { get; private set; }
    public long? TransferredByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private GraveOwnershipHistory() { }

    public GraveOwnershipHistory(
        long graveId, long? previousOwnerId, long newOwnerId,
        string transferType, string? reason, long? transferredByUserId)
    {
        GraveId = graveId;
        PreviousOwnerId = previousOwnerId;
        NewOwnerId = newOwnerId;
        TransferType = transferType ?? throw new ArgumentNullException(nameof(transferType));
        Reason = reason;
        TransferredByUserId = transferredByUserId;
        TransferredAt = DateTime.UtcNow;
    }
}
