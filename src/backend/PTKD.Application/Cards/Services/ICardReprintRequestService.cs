using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Cards.DTOs;

namespace PTKD.Application.Cards.Services;

public interface ICardReprintRequestService
{
    Task<CardReprintRequestDto> CreateRequestAsync(CreateCardReprintRequest request, long actorUserId, CancellationToken ct = default);
    Task<CardReprintRequestDto?> GetRequestByIdAsync(long id, long companyId, CancellationToken ct = default);
    Task<IEnumerable<CardReprintRequestDto>> GetRequestsAsync(long companyId, CancellationToken ct = default);
}
