using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Tags.DTOs;

namespace PTKD.Application.Tags.Services;

public interface ITagService
{
    /// <summary>Danh mục thẻ theo loại (CUSTOMER/GRAVE). includeInactive=false chỉ trả thẻ đang dùng.</summary>
    Task<IReadOnlyList<TagDto>> ListTagsAsync(string tagType, bool includeInactive, CancellationToken ct = default);

    Task<TagDto> CreateTagAsync(CreateTagRequest request, long actorUserId, CancellationToken ct = default);
    Task<TagDto> UpdateTagAsync(long id, UpdateTagRequest request, long actorUserId, CancellationToken ct = default);
    Task DeactivateTagAsync(long id, long actorUserId, CancellationToken ct = default);

    /// <summary>Đặt lại toàn bộ tập thẻ của khách hàng (tạo thẻ mới theo tên nếu cần). Trả về tập thẻ sau khi lưu.</summary>
    Task<IReadOnlyList<TagDto>> SetCustomerTagsAsync(long customerId, SetEntityTagsRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>Đặt lại toàn bộ tập thẻ của phần mộ (tạo thẻ mới theo tên nếu cần). Trả về tập thẻ sau khi lưu.</summary>
    Task<IReadOnlyList<TagDto>> SetGraveTagsAsync(long graveId, SetEntityTagsRequest request, long actorUserId, CancellationToken ct = default);
}
