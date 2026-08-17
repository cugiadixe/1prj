using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Cards.DTOs;

namespace PTKD.Application.Cards.Services;

public interface ICardService
{
    /// <summary>
    /// Tạo/cấp thẻ mộ mới từ một phần mộ. <paramref name="companyId"/> là công ty người gọi khai
    /// (X-Company-Id); service kiểm mộ THUỘC đúng công ty đó (qua nghĩa trang) rồi mới tạo thẻ.
    /// </summary>
    Task<CardDto> CreateCardFromGraveAsync(long graveId, long companyId, long? serviceId, long actorUserId, CancellationToken ct = default);
    Task<CardDto?> GetByIdAsync(long id, long companyId, CancellationToken ct = default);
    Task<IEnumerable<CardDto>> GetByCompanyAsync(long companyId, CancellationToken ct = default);
}
