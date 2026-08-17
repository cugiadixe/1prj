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
    Task<CardReprintRequestDto> SubmitAsync(long id, long companyId, long actorUserId, CancellationToken ct = default);
    Task<CardReprintRequestDto> ApproveStepAsync(long id, long stepId, string targetVersion, string reason, string comment, long companyId, long actorUserId, CancellationToken ct = default);
    Task<CardReprintRequestDto> RejectStepAsync(long id, long stepId, string targetVersion, string reason, string comment, long companyId, long actorUserId, CancellationToken ct = default);
    Task<CardReprintRequestDto> CreatePaymentAsync(long id, string paymentMethod, long companyId, long actorUserId, CancellationToken ct = default);
    Task<object?> GetPaymentStatusAsync(long id, long companyId, CancellationToken ct = default);
    Task<CardReprintRequestDto> MarkPrintedAsync(long id, long companyId, long actorUserId, CancellationToken ct = default);
    /// <summary>In lần đầu trực tiếp (không duyệt, không phí) — chỉ cho yêu cầu INITIAL_PRINT.</summary>
    Task<CardReprintRequestDto> PrintInitialAsync(long id, long companyId, long actorUserId, CancellationToken ct = default);
    Task<CardReprintRequestDto> MarkReleasedAsync(long id, long companyId, long actorUserId, CancellationToken ct = default);
}
