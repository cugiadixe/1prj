using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.CardWatermarks;

public interface ICardWatermarkService
{
    Task<IReadOnlyList<CardWatermarkDto>> ListAsync(long companyId, CancellationToken ct = default);
    Task<CardWatermarkDto> UploadAsync(long companyId, string name, string contentType, byte[] imageBytes, long actorUserId, CancellationToken ct = default);
    Task<CardWatermarkContent?> GetContentAsync(long id, long companyId, CancellationToken ct = default);
    Task DeleteAsync(long id, long companyId, long actorUserId, CancellationToken ct = default);
}
