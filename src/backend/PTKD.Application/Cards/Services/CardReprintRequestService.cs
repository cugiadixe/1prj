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

namespace PTKD.Application.Cards.Services;

public class CardReprintRequestService : ICardReprintRequestService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public CardReprintRequestService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
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
