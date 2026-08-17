using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Application.CardWatermarks;

public class CardWatermarkService : ICardWatermarkService
{
    private const long MaxBytes = 3 * 1024 * 1024; // 3MB
    private static readonly HashSet<string> AllowedTypes = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/jpg",
    };

    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public CardWatermarkService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<CardWatermarkDto>> ListAsync(long companyId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        return await db.CardWatermarks.AsNoTracking()
            .Where(w => w.CompanyId == companyId && w.IsActive)
            .OrderBy(w => w.Name)
            .Select(w => new CardWatermarkDto { Id = w.Id, Name = w.Name, ContentType = w.ContentType, CreatedAt = w.CreatedAt })
            .ToListAsync(ct);
    }

    public async Task<CardWatermarkDto> UploadAsync(long companyId, string name, string contentType, byte[] imageBytes, long actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("WATERMARK_NAME_REQUIRED", "Vui lòng đặt tên cho mẫu hoa văn.");
        if (imageBytes == null || imageBytes.Length == 0)
            throw new BusinessRuleValidationException("WATERMARK_EMPTY", "Thiếu file ảnh.");
        if (imageBytes.Length > MaxBytes)
            throw new BusinessRuleValidationException("WATERMARK_TOO_LARGE", "Ảnh vượt quá 3MB.");
        if (!AllowedTypes.Contains(contentType))
            throw new BusinessRuleValidationException("WATERMARK_TYPE", "Chỉ chấp nhận ảnh PNG hoặc JPEG.");

        await using var db = _dbContextFactory.CreateDbContext();
        var entity = CardWatermark.Create(companyId, name.Trim(), contentType, imageBytes, actorUserId);
        db.CardWatermarks.Add(entity);
        await db.SaveChangesAsync(ct);
        return new CardWatermarkDto { Id = entity.Id, Name = entity.Name, ContentType = entity.ContentType, CreatedAt = entity.CreatedAt };
    }

    public async Task<CardWatermarkContent?> GetContentAsync(long id, long companyId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        var w = await db.CardWatermarks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId && x.IsActive, ct);
        return w == null ? null : new CardWatermarkContent { Bytes = w.ImageBytes, ContentType = w.ContentType };
    }

    public async Task DeleteAsync(long id, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        var w = await db.CardWatermarks.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);
        if (w == null)
            throw new EntityNotFoundException("WATERMARK_NOT_FOUND", "Không tìm thấy mẫu hoa văn.");

        // Gỡ mọi nghĩa trang đang trỏ tới mẫu này để không còn tham chiếu treo.
        var code = $"UPLOAD:{id}";
        var refs = await db.Cemeteries.Where(c => c.CompanyId == companyId && c.CardWatermarkCode == code).ToListAsync(ct);
        foreach (var cem in refs) cem.SetCardWatermark(null, actorUserId);

        db.CardWatermarks.Remove(w);
        await db.SaveChangesAsync(ct);
    }
}
