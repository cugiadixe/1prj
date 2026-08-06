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

        var workflowRequest = new CreateWorkflowInstanceRequest
        {
            ProcessCode = "CARD_REPRINT",
            BusinessEntityType = "CardReprintRequest",
            BusinessEntityId = request.Id,
            CompanyId = companyId,
            PayloadJson = JsonSerializer.Serialize(new { CardId = request.CardId, ReasonCode = request.ReasonCode })
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

        var serviceType = await dbContext.ServiceTypes.FirstOrDefaultAsync(st => st.Code == "CARD_REPRINT" && st.IsActive, ct);
        if (serviceType == null)
            throw new BusinessRuleValidationException("SERVICE_TYPE_NOT_FOUND", "Card Reprint service type is not configured or inactive. Cannot process payment.");

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
        await using var dbContext = _dbContextFactory.CreateDbContext();
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

        request.SetPrinted(actorUserId);
        await dbContext.SaveChangesAsync(ct);

        return MapToDto(request);
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
