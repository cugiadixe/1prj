using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Cards.DTOs;
using PTKD.Domain.Entities;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.PaymentManagement.DTOs;
using PTKD.Application.PaymentManagement.Services;
using PTKD.Application.Workflows.DTOs;
using PTKD.Application.Workflows.Services;
using System.Text.Json;

namespace PTKD.Application.Cards.Services;

public class CardReprintRequestService : ICardReprintRequestService
{
    // Mã loại dịch vụ tính phí in lại (Service_Types.code). Trước đây tra nhầm 'CARD_REPRINT'
    // (không tồn tại) → tạo thanh toán luôn văng lỗi. Mã thật trong danh mục là 'IN_THE' (50.000đ).
    private const string ReprintFeeServiceCode = "IN_THE";

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly IWorkflowRuntimeService _workflowRuntimeService;
    private readonly IPaymentTransactionService _paymentTransactionService;

    public CardReprintRequestService(
        IOrganizationDbContextFactory dbContextFactory,
        IWorkflowRuntimeService workflowRuntimeService,
        IPaymentTransactionService paymentTransactionService)
    {
        _dbContextFactory = dbContextFactory;
        _workflowRuntimeService = workflowRuntimeService;
        _paymentTransactionService = paymentTransactionService;
    }

    public async Task<CardReprintRequestDto> CreateRequestAsync(CreateCardReprintRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();

        var card = await _dbContext.Cards
            .FirstOrDefaultAsync(c => c.Id == request.CardId && c.CompanyId == request.CompanyId, ct);
            
        if (card == null)
            throw new EntityNotFoundException("CARD_NOT_FOUND", "Card not found or does not belong to the specified company.");

        var requestType = card.PrintCount == 0 ? CardReprintRequest.TypeInitialPrint : CardReprintRequest.TypeReprint;
        var reprintNumber = card.PrintCount + 1;

        var entity = CardReprintRequest.CreateDraft(
            request.CompanyId,
            request.CardId,
            actorUserId, // requester
            requestType,
            reprintNumber,
            request.ReasonCode,
            request.Notes,
            actorUserId // created by
        );

        _dbContext.CardReprintRequests.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(entity);
    }

    public async Task<CardReprintRequestDto?> GetRequestByIdAsync(long id, long companyId, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();

        var entity = await _dbContext.CardReprintRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

        if (entity == null) return null;
        return MapToDto(entity);
    }

    public async Task<IEnumerable<CardReprintRequestDto>> GetRequestsAsync(long companyId, CancellationToken ct = default)
    {
        await using var _dbContext = _dbContextFactory.CreateDbContext();

        var entities = await _dbContext.CardReprintRequests
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(MapToDto);
    }

    public async Task<CardReprintRequestDto> SubmitAsync(long id, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CardReprintRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Card reprint request not found.");

        // In LẦN ĐẦU không qua duyệt — dùng đường in trực tiếp (PrintInitialAsync), không mở workflow.
        if (request.RequestType == CardReprintRequest.TypeInitialPrint)
            throw new BusinessRuleValidationException(
                "INITIAL_PRINT_NO_APPROVAL",
                "In lần đầu không cần duyệt. Vui lòng dùng chức năng in trực tiếp thay vì gửi duyệt.");

        var workflowRequest = new CreateWorkflowInstanceRequest
        {
            ProcessCode = "CARD_REPRINT",
            BusinessEntityType = "CardReprintRequest",
            BusinessEntityId = request.Id,
            CompanyId = companyId,
            PayloadJson = JsonSerializer.Serialize(new
            {
                CardId = request.CardId,
                request.RequestType,
                request.ReprintNumber,
                request.ReasonCode
            })
        };

        var instance = await _workflowRuntimeService.CreateInstanceAsync(workflowRequest, actorUserId, ct);

        request.SetSubmitted(instance.Id);
        await dbContext.SaveChangesAsync(ct);

        return MapToDto(request);
    }

    public async Task<CardReprintRequestDto> ApproveStepAsync(long id, long stepId, string targetVersion, string reason, string comment, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CardReprintRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null || !request.WorkflowInstanceId.HasValue) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Card reprint request not found or not submitted.");

        var actionRequest = new ApprovalActionRequest
        {
            TargetVersion = targetVersion,
            Reason = reason,
            Comment = comment
        };

        var instance = await _workflowRuntimeService.ApproveStepAsync(request.WorkflowInstanceId.Value, stepId, actionRequest, actorUserId, ct);

        // Handlers will update domain status if execution happens

        // Reload to get latest status if execution handler ran synchronously
        await dbContext.Entry(request).ReloadAsync(ct);

        return MapToDto(request);
    }

    public async Task<CardReprintRequestDto> RejectStepAsync(long id, long stepId, string targetVersion, string reason, string comment, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CardReprintRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null || !request.WorkflowInstanceId.HasValue) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Card reprint request not found or not submitted.");

        var actionRequest = new ApprovalActionRequest
        {
            TargetVersion = targetVersion,
            Reason = reason,
            Comment = comment
        };

        var instance = await _workflowRuntimeService.RejectStepAsync(request.WorkflowInstanceId.Value, stepId, actionRequest, actorUserId, ct);

        if (instance.InstanceStatus == "REJECTED")
        {
            request.SetRejected();
            await dbContext.SaveChangesAsync(ct);
        }

        return MapToDto(request);
    }

    public async Task<CardReprintRequestDto> CreatePaymentAsync(long id, string paymentMethod, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CardReprintRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Card reprint request not found.");

        if (request.Status != CardReprintRequest.StatusApproved)
            throw new BusinessRuleValidationException("INVALID_STATUS", "Payment can only be created for APPROVED requests.");

        var serviceType = await dbContext.ServiceTypes.FirstOrDefaultAsync(st => st.Code == ReprintFeeServiceCode && st.IsActive, ct);
        if (serviceType == null)
            throw new BusinessRuleValidationException("SERVICE_TYPE_NOT_FOUND", "Loại dịch vụ phí in lại thẻ chưa được cấu hình hoặc đã ngừng. Không thể tạo thanh toán.");

        var paymentDraftRequest = new CreatePaymentDraftRequest
        {
            CustomerId = request.RequesterId, // Reusing requester as customer for payment
            CompanyId = companyId,
            PaymentMethod = paymentMethod,
            PaymentDate = DateTime.UtcNow,
            Notes = $"Card Reprint for Card {request.CardId}",
            Items = new List<CreatePaymentItemRequest>
            {
                new CreatePaymentItemRequest
                {
                    ServiceId = serviceType.Id,
                    Amount = serviceType.StandardPrice,
                    Description = $"Reprint #{request.ReprintNumber}"
                }
            }
        };

        var transaction = await _paymentTransactionService.CreateDraftAsync(paymentDraftRequest, actorUserId, ct);
        var itemId = transaction.Items.FirstOrDefault()?.Id ?? 0;

        request.SetPaymentDraft(transaction.Id, itemId, serviceType.StandardPrice, serviceType.StandardPriceCurrency);
        await dbContext.SaveChangesAsync(ct);

        return MapToDto(request);
    }

    public async Task<object?> GetPaymentStatusAsync(long id, long companyId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CardReprintRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null || !request.PaymentTransactionId.HasValue) return null;

        return await _paymentTransactionService.GetByIdAsync(request.PaymentTransactionId.Value, ct);
    }

    public async Task<CardReprintRequestDto> MarkPrintedAsync(long id, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            await using var tx = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var request = await dbContext.CardReprintRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
            if (request == null) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Card reprint request not found.");

            if (request.Status == CardReprintRequest.StatusPendingPayment)
            {
                if (!request.PaymentTransactionId.HasValue)
                    throw new BusinessRuleValidationException("NO_PAYMENT", "No payment transaction linked.");

                var payment = await _paymentTransactionService.GetByIdAsync(request.PaymentTransactionId.Value, ct);
                if (payment == null || payment.Status != "CONFIRMED")
                    throw new BusinessRuleValidationException("PAYMENT_NOT_CONFIRMED", "Payment must be CONFIRMED before printing.");

                request.SetPaid();
            }

            var card = await dbContext.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId && c.CompanyId == companyId, ct);
            if (card == null) throw new EntityNotFoundException("CARD_NOT_FOUND", "Card not found.");

            request.SetPrinted(actorUserId);

            // Ghi nhật ký in + tăng đếm (cộng dồn) trong CÙNG giao dịch. Số thứ tự in lấy tại đây,
            // không tin số lưu lúc tạo yêu cầu.
            var sequence = card.PrintCount + 1;
            card.IncrementPrintCount(actorUserId);
            dbContext.CardPrintHistory.Add(CardPrintHistory.Create(
                card.Id, companyId, sequence, CardPrintHistory.TypeReprint,
                request.Id, request.WorkflowInstanceId, actorUserId, request.ReasonCode, request.Notes));

            await dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return MapToDto(request);
        });
    }

    /// <summary>
    /// In LẦN ĐẦU: bỏ qua duyệt + phí, in thẳng. Chốt an toàn trong giao dịch Serializable —
    /// đọc lại số lần in, chỉ cho qua nếu thẻ CHƯA in lần nào; nếu đã in thì buộc đi đường in lại.
    /// UNIQUE 1 dòng INITIAL/thẻ khoá nốt lỗ hai lần in đầu song song.
    /// </summary>
    public async Task<CardReprintRequestDto> PrintInitialAsync(long id, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            await using var tx = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var request = await dbContext.CardReprintRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
            if (request == null) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Card reprint request not found.");
            if (request.RequestType != CardReprintRequest.TypeInitialPrint)
                throw new BusinessRuleValidationException("NOT_INITIAL", "Chức năng in trực tiếp chỉ áp dụng cho yêu cầu in lần đầu.");

            var card = await dbContext.Cards.FirstOrDefaultAsync(c => c.Id == request.CardId && c.CompanyId == companyId, ct);
            if (card == null) throw new EntityNotFoundException("CARD_NOT_FOUND", "Card not found.");

            if (card.PrintCount != 0)
                throw new BusinessRuleValidationException(
                    "CARD_ALREADY_PRINTED",
                    "Thẻ này đã được in. Việc in thêm là IN LẠI — vui lòng tạo yêu cầu in lại (cần duyệt + phí).");

            request.SetPrintedInitial(actorUserId);
            card.IncrementPrintCount(actorUserId);
            dbContext.CardPrintHistory.Add(CardPrintHistory.Create(
                card.Id, companyId, 1, CardPrintHistory.TypeInitial,
                request.Id, null, actorUserId, request.ReasonCode, request.Notes));

            await dbContext.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return MapToDto(request);
        });
    }

    public async Task<CardReprintRequestDto> MarkReleasedAsync(long id, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CardReprintRequests.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
        if (request == null) throw new EntityNotFoundException("REQUEST_NOT_FOUND", "Card reprint request not found.");

        request.SetReleased(actorUserId);
        await dbContext.SaveChangesAsync(ct);

        return MapToDto(request);
    }

    private static CardReprintRequestDto MapToDto(CardReprintRequest entity)
    {
        return new CardReprintRequestDto
        {
            Id = entity.Id,
            CompanyId = entity.CompanyId,
            CardId = entity.CardId,
            RequesterId = entity.RequesterId,
            RequestType = entity.RequestType,
            ReprintNumber = entity.ReprintNumber,
            FeeAmount = entity.FeeAmount,
            FeeCurrency = entity.FeeCurrency,
            ReasonCode = entity.ReasonCode,
            WorkflowInstanceId = entity.WorkflowInstanceId,
            PaymentTransactionId = entity.PaymentTransactionId,
            ServiceItemId = entity.ServiceItemId,
            Status = entity.Status,
            Notes = entity.Notes,
            PrintedAt = entity.PrintedAt,
            PrintedByUserId = entity.PrintedByUserId,
            ReleasedAt = entity.ReleasedAt,
            ReleasedByUserId = entity.ReleasedByUserId,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersion = entity.RowVersion
        };
    }
}
