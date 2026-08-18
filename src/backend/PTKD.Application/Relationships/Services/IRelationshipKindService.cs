using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Relationships.DTOs;

namespace PTKD.Application.Relationships.Services;

public interface IRelationshipKindService
{
    Task<IReadOnlyList<RelationshipKindDetailDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Tạo loại quan hệ mới. Đối xứng → 1 loại tự nghịch đảo; bất đối xứng → cặp 2 loại nối nhau.</summary>
    Task<RelationshipKindDetailDto> CreateAsync(CreateRelationshipKindRequest request, long actorUserId, CancellationToken ct = default);

    Task UpdateAsync(string kindCode, UpdateRelationshipKindRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>Xoá loại (và loại nghịch đảo nếu là cặp). Chặn nếu là loại hệ thống hoặc đang bị tham chiếu.</summary>
    Task DeleteAsync(string kindCode, long actorUserId, CancellationToken ct = default);
}
