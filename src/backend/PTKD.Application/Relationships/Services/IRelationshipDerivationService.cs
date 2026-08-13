using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Relationships.Services;

/// <summary>Kết quả suy diễn quan hệ chủ mộ ↔ một cốt.</summary>
public sealed record DerivedRelationship(
    long OccupantCustomerId,
    string RelationKind,
    string OwnerRelationshipLabel,     // chủ mộ gọi cốt là gì (vd "Ông nội")
    string DeceasedRelationshipLabel,  // cốt gọi chủ mộ là gì (vd "Cháu nội (trai)")
    bool NeedsConfirmation);           // true nếu suy diễn không chắc, cần người xác nhận

public interface IRelationshipDerivationService
{
    /// <summary>
    /// Suy diễn nhãn quan hệ từ góc nhìn của <paramref name="ownerCustomerId"/> đến từng
    /// khách hàng trong <paramref name="occupantCustomerIds"/>.
    /// Ưu tiên cạnh trực tiếp → ghép 2 bậc (nội/ngoại theo giới tính người trung gian) → OTHER.
    /// </summary>
    Task<IReadOnlyList<DerivedRelationship>> DeriveOwnerToOccupantsAsync(
        long ownerCustomerId, IReadOnlyList<long> occupantCustomerIds, CancellationToken ct = default);
}
