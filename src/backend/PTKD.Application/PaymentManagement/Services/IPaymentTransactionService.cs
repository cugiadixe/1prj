using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Common.Models;
using PTKD.Application.PaymentManagement.DTOs;

namespace PTKD.Application.PaymentManagement.Services;

public interface IPaymentTransactionService
{
    Task<PaymentTransactionDto> CreateDraftAsync(CreatePaymentDraftRequest request, long actorUserId, CancellationToken ct = default);
    Task<PaymentTransactionDto> ConfirmAsync(long id, ConfirmPaymentRequest request, long actorUserId, CancellationToken ct = default);
    Task<PaymentTransactionDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PagedResult<PaymentTransactionListDto>> ListAsync(long companyId, long? customerId, string? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
    Task<PaymentTransactionDto> CorrectConfirmedAsync(long id, CorrectPaymentRequest request, long actorUserId, CancellationToken ct = default);
    Task SoftDeleteDraftAsync(long id, SoftDeletePaymentRequest request, long actorUserId, CancellationToken ct = default);
}
