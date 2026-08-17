using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Cards.DTOs;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Application.Cards.Services;

public class CardService : ICardService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public CardService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<CardDto> CreateCardFromGraveAsync(long graveId, long companyId, long? serviceId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var tx = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            // Công ty của mộ = công ty của nghĩa trang chứa mộ (mộ thuộc công ty QUA nghĩa trang).
            var grave = await context.Graves.AsNoTracking().FirstOrDefaultAsync(g => g.Id == graveId, ct);
            if (grave == null)
                throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Không tìm thấy phần mộ.");

            var graveCompanyId = await context.Cemeteries.AsNoTracking()
                .Where(cem => cem.Id == grave.CemeteryId)
                .Select(cem => (long?)cem.CompanyId)
                .FirstOrDefaultAsync(ct);
            if (graveCompanyId is null)
                throw new EntityNotFoundException("CEMETERY_NOT_FOUND", "Nghĩa trang của phần mộ không tồn tại.");

            // Chặn cấp thẻ chéo công ty: mộ phải thuộc đúng công ty đang khai.
            if (graveCompanyId.Value != companyId)
                throw new PermissionDeniedException("CARD_GRAVE_COMPANY_MISMATCH",
                    "Phần mộ không thuộc công ty đang chọn — không thể cấp thẻ.");

            // 1 mộ = 1 thẻ hoạt động: chặn tạo trùng thẻ cho cùng phần mộ.
            var graveCode = grave.GraveCode;
            var exists = await context.Cards
                .AnyAsync(c => c.CompanyId == companyId && c.GraveId == graveCode && c.Status == Card.StatusActive, ct);
            if (exists)
                throw new BusinessRuleValidationException("CARD_ALREADY_EXISTS",
                    "Phần mộ này đã có thẻ đang hoạt động. Mỗi mộ chỉ có một thẻ.");

            var cardNumber = await GenerateCardNumberAsync(context, companyId, ct);

            var card = Card.Create(companyId, graveCode, serviceId, actorUserId, cardNumber);
            context.Cards.Add(card);
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return MapToDto(card);
        });
    }

    public async Task<CardDto?> GetByIdAsync(long id, long companyId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var card = await context.Cards.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId, ct);
        return card == null ? null : MapToDto(card);
    }

    public async Task<IEnumerable<CardDto>> GetByCompanyAsync(long companyId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var cards = await context.Cards.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);
        return cards.Select(MapToDto);
    }

    /// <summary>
    /// Sinh SỐ THẺ tuần tự theo công ty (max số hiện có + 1). Chạy trong giao dịch Serializable của
    /// nơi gọi nên tránh cấp trùng số. Bản MVP: số thuần tăng dần; chưa gắn tiền tố năm/xí nghiệp.
    /// </summary>
    private static async Task<string> GenerateCardNumberAsync(IOrganizationDbContext context, long companyId, CancellationToken ct)
    {
        var numbers = await context.Cards
            .Where(c => c.CompanyId == companyId && c.CardNumber != null)
            .Select(c => c.CardNumber!)
            .ToListAsync(ct);

        var max = 0;
        foreach (var n in numbers)
            if (int.TryParse(n, out var v) && v > max) max = v;

        return (max + 1).ToString();
    }

    private static CardDto MapToDto(Card c) => new()
    {
        Id = c.Id,
        CompanyId = c.CompanyId,
        GraveId = c.GraveId,
        CardNumber = c.CardNumber,
        ServiceId = c.ServiceId,
        PrintCount = c.PrintCount,
        Status = c.Status,
        CreatedAt = c.CreatedAt
    };
}
