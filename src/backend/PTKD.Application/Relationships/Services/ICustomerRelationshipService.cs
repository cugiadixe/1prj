using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Relationships.DTOs;

namespace PTKD.Application.Relationships.Services;

public interface ICustomerRelationshipService
{
    /// <summary>Danh mục loại quan hệ cho dropdown khai báo (bỏ SIBLING_OLDER/YOUNGER — suy theo tuổi).</summary>
    Task<IReadOnlyList<RelationshipKindDto>> GetKindsAsync(CancellationToken ct = default);

    /// <summary>Liệt kê toàn bộ quan hệ (mỗi cặp một dòng, canonical from&lt;to) cho trang quản lý.</summary>
    Task<RelationshipPagedResult<RelationshipListItemDto>> SearchAllAsync(RelationshipSearchRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>Các quan hệ đã khai của một khách (cạnh from = khách này).</summary>
    Task<IReadOnlyList<CustomerRelationshipDto>> GetForCustomerAsync(long customerId, long actorUserId, CancellationToken ct = default);

    /// <summary>Khai "người thân LÀ &lt;kind&gt; của khách này" — ghi cả cạnh thuận và cạnh nghịch đảo.</summary>
    Task<CustomerRelationshipDto> CreateAsync(long customerId, CreateCustomerRelationshipRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>Xoá quan hệ (cả cạnh nghịch đảo).</summary>
    Task DeleteAsync(long customerId, long relationshipId, long actorUserId, CancellationToken ct = default);
}
