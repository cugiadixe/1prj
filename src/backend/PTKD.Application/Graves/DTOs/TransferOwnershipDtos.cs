using System;

namespace PTKD.Application.Graves.DTOs;

/// <summary>Yêu cầu chuyển quyền sở hữu phần mộ.</summary>
public class TransferOwnershipRequest
{
    public long NewOwnerCustomerId { get; set; }
    public string TransferType { get; set; } = "SALE";   // SALE / INHERITANCE / DEATH / CORRECTION
    public string? Reason { get; set; }
    public string TargetVersion { get; set; } = null!;    // RowVersion base64 để kiểm tra tương tranh
}

/// <summary>Kết quả chuyển quyền: mộ sau khi đổi chủ + tóm tắt tái suy diễn quan hệ.</summary>
public class TransferOwnershipResultDto
{
    public GraveDetailDto Grave { get; set; } = null!;
    public long OwnershipHistoryId { get; set; }
    public int OccupantsRederived { get; set; }
    public int OccupantsNeedingConfirmation { get; set; }
}

/// <summary>Yêu cầu xử lý khi chủ mộ qua đời: đánh dấu Chết + tự chuyển mọi mộ cho người thừa kế.</summary>
public class OwnerDeathRequest
{
    public long DeceasedCustomerId { get; set; }
    public System.DateTime? DeathDateSolar { get; set; }
    public long HeirCustomerId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>Tóm tắt luồng chủ mất: số mộ đã tự chuyển + số cốt tái suy diễn.</summary>
public class OwnerDeathResultDto
{
    public long DeceasedCustomerId { get; set; }
    public long HeirCustomerId { get; set; }
    public int GravesOwned { get; set; }
    public int GravesTransferred { get; set; }
    public int OccupantsRederived { get; set; }
}

/// <summary>Một dòng lịch sử chuyển quyền sở hữu mộ.</summary>
public class OwnershipHistoryItemDto
{
    public long Id { get; set; }
    public long? PreviousOwnerId { get; set; }
    public string? PreviousOwnerName { get; set; }
    public long NewOwnerId { get; set; }
    public string? NewOwnerName { get; set; }
    public string TransferType { get; set; } = null!;
    public string? Reason { get; set; }
    public System.DateTime TransferredAt { get; set; }
}
