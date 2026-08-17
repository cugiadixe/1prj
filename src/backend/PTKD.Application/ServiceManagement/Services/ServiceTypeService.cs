using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Common.Models;
using PTKD.Application.ServiceManagement.DTOs;
using PTKD.Domain.Entities;

namespace PTKD.Application.ServiceManagement.Services;

public class ServiceTypeService : IServiceTypeService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public ServiceTypeService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedResult<ServiceTypeDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var query = db.ServiceTypes.AsNoTracking().Where(st => st.IsActive);
        var totalCount = await query.LongCountAsync(ct);
        var items = await query
            .OrderBy(st => st.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(st => MapToDto(st))
            .ToArrayAsync(ct);

        return new PagedResult<ServiceTypeDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceTypeDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        var entity = await db.ServiceTypes.AsNoTracking().FirstOrDefaultAsync(st => st.Id == id, ct);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<ServiceTypeDto> CreateAsync(CreateServiceTypeRequest request, long userId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var codeExists = await db.ServiceTypes.AnyAsync(st => st.Code == request.Code, ct);
        if (codeExists)
            throw new InvalidOperationException("A service type with this code already exists.");

        var entity = new ServiceType(
            request.Code,
            request.Name,
            request.Description,
            request.StandardPrice,
            request.CycleDurationMonths,
            request.IsCarePackage,
            userId,
            request.PricingBasis);

        db.ServiceTypes.Add(entity);
        await db.SaveChangesAsync(ct);

        var priceHistory = new ServicePriceHistory(
            entity.Id,
            request.StandardPrice,
            DateTime.UtcNow,
            userId,
            "Initial standard price");

        db.ServicePriceHistories.Add(priceHistory);
        await db.SaveChangesAsync(ct);

        return MapToDto(entity);
    }

    public async Task<ServiceTypeDto> UpdateAsync(long id, UpdateServiceTypeRequest request, long userId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var entity = await db.ServiceTypes.FirstOrDefaultAsync(st => st.Id == id, ct)
            ?? throw new InvalidOperationException("Service type not found.");

        db.Entry(entity).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
        entity.Update(request.Name, request.Description, request.CycleDurationMonths, request.IsCarePackage, request.PricingBasis);
        await db.SaveChangesAsync(ct);

        return MapToDto(entity);
    }

    public async Task<ServiceTypeDto> DeactivateAsync(long id, string rowVersion, long userId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var entity = await db.ServiceTypes.FirstOrDefaultAsync(st => st.Id == id, ct)
            ?? throw new InvalidOperationException("Service type not found.");

        db.Entry(entity).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
        entity.Deactivate();
        await db.SaveChangesAsync(ct);

        return MapToDto(entity);
    }

    private static ServiceTypeDto MapToDto(ServiceType entity)
    {
        return new ServiceTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            StandardPrice = entity.StandardPrice,
            StandardPriceCurrency = entity.StandardPriceCurrency,
            CycleDurationMonths = entity.CycleDurationMonths,
            IsCarePackage = entity.IsCarePackage,
            PricingBasis = entity.PricingBasis,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            RowVersion = Convert.ToBase64String(entity.RowVersion)
        };
    }
}
