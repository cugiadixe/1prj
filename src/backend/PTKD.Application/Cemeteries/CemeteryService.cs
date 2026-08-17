using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;

namespace PTKD.Application.Cemeteries;

public class CemeteryService : ICemeteryService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public CemeteryService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<CemeteryDto>> GetByCompanyAsync(long companyId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        var items = await db.Cemeteries.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .Select(c => new CemeteryDto
            {
                Id = c.Id,
                CemeteryCode = c.CemeteryCode,
                Name = c.Name,
                Address = c.Address,
                IsActive = c.IsActive,
                CardWatermarkCode = c.CardWatermarkCode,
            })
            .ToListAsync(ct);
        return items;
    }

    public async Task SetWatermarkAsync(long cemeteryId, string? watermarkCode, long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        var cemetery = await db.Cemeteries.FirstOrDefaultAsync(c => c.Id == cemeteryId && c.CompanyId == companyId, ct);
        if (cemetery == null)
            throw new EntityNotFoundException("CEMETERY_NOT_FOUND", "Không tìm thấy nghĩa trang trong công ty đang chọn.");

        cemetery.SetCardWatermark(watermarkCode, actorUserId);
        await db.SaveChangesAsync(ct);
    }
}
