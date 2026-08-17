using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Graves.DTOs;

namespace PTKD.Application.Graves.Services;

public interface IGraveService
{
    Task<PagedResult<GraveListItemDto>> SearchGravesAsync(GraveSearchRequest request, long actorUserId, CancellationToken ct = default);
    /// <summary>Bảng tổng hợp giấy tờ/tài liệu theo mộ (đếm theo loại), lọc theo công ty.</summary>
    Task<PagedResult<GraveAttachmentSummaryDto>> GetAttachmentSummaryAsync(GraveAttachmentSummaryRequest request, long actorUserId, CancellationToken ct = default);
    /// <summary>Danh sách người từng tải tài liệu lên (trong phạm vi công ty) — để lọc.</summary>
    Task<System.Collections.Generic.IReadOnlyList<AttachmentUploaderDto>> GetAttachmentUploadersAsync(long actorUserId, CancellationToken ct = default);
    Task<GraveDetailDto?> GetGraveByIdAsync(long id, long actorUserId, CancellationToken ct = default);
    Task<GraveDetailDto> CreateGraveAsync(CreateGraveRequest request, long actorUserId, CancellationToken ct = default);
    Task<GraveDetailDto> UpdateGraveAsync(long id, UpdateGraveRequest request, long actorUserId, CancellationToken ct = default);
    Task<GraveOccupantDto> AddOccupantAsync(long graveId, CreateGraveOccupantRequest request, long actorUserId, CancellationToken ct = default);
    Task<GraveOccupantDto> UpdateOccupantAsync(long graveId, long occupantId, UpdateGraveOccupantRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>Thêm liên hệ khẩn cấp (là khách hàng) cho phần mộ — ưu tiên tự gán kế tiếp.</summary>
    Task<GraveEmergencyContactDto> AddEmergencyContactAsync(long graveId, CreateGraveEmergencyContactRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>Sửa liên hệ khẩn cấp (đổi khách hàng / ghi chú quan hệ).</summary>
    Task<GraveEmergencyContactDto> UpdateEmergencyContactAsync(long graveId, long contactId, UpdateGraveEmergencyContactRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>Xóa liên hệ khẩn cấp khỏi phần mộ.</summary>
    Task RemoveEmergencyContactAsync(long graveId, long contactId, long actorUserId, CancellationToken ct = default);

    /// <summary>Chuyển quyền sở hữu mộ sang chủ mới, ghi lịch sử và TÁI SUY DIỄN nhãn quan hệ của các cốt.</summary>
    Task<TransferOwnershipResultDto> TransferOwnershipAsync(long graveId, TransferOwnershipRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>Chủ mộ qua đời: đánh dấu Chết + tự động chuyển mọi mộ đang sở hữu cho người thừa kế (type=DEATH) và tái suy diễn quan hệ.</summary>
    Task<OwnerDeathResultDto> ProcessOwnerDeathAsync(OwnerDeathRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>Lịch sử chuyển quyền sở hữu của một phần mộ (mới nhất trước).</summary>
    Task<System.Collections.Generic.IReadOnlyList<OwnershipHistoryItemDto>> GetOwnershipHistoryAsync(long graveId, long actorUserId, CancellationToken ct = default);
}
