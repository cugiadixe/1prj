using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.CarePackages.DTOs;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Common.Models;
using PTKD.Domain.Entities;

namespace PTKD.Application.CarePackages.Services;

public interface ICarePackageRequestService
{
    Task<PagedResult<CarePackageRequestDto>> ListAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<CarePackageRequestDto?> GetByIdAsync(
        long companyId,
        long id,
        CancellationToken ct = default);

    Task<CarePackageRequestDto> CreateAsync(
        long companyId,
        CreateCarePackageRequest request,
        long userId,
        CancellationToken ct = default);
}

public class CarePackageRequestService : ICarePackageRequestService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public CarePackageRequestService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedResult<CarePackageRequestDto>> ListAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var query = db.CarePackageRequests
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId);

        var totalCount = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToArrayAsync(ct);

        return new PagedResult<CarePackageRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CarePackageRequestDto?> GetByIdAsync(
        long companyId,
        long id,
        CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var entity = await db.CarePackageRequests
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<CarePackageRequestDto> CreateAsync(
        long companyId,
        CreateCarePackageRequest request,
        long userId,
        CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        // 1. Guards
        if (request.Item == null)
            throw new ArgumentException("At least one item is required.");

        var customerExists = await db.Customers.AnyAsync(x => x.Id == request.CustomerId, ct);
        if (!customerExists)
            throw new InvalidOperationException("Customer not found.");

        decimal unitPriceSnapshot = 0m;
        
        // Lookup effective price using Service Foundation if ServiceId is provided.
        // If ServiceId is passed, it represents the specific instance of the service we're renewing/creating against.
        // Wait, normally we look up standard price from ServiceType or ServicePriceHistories.
        // For B1, we simulate effective price lookup. If a Service is linked, we can use its AppliedPrice.
        if (request.ServiceId.HasValue)
        {
            var service = await db.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.ServiceId.Value && x.CompanyId == companyId, ct);
                
            if (service == null)
                throw new InvalidOperationException("Service not found.");
                
            unitPriceSnapshot = service.AppliedPrice;
        }
        else
        {
            // Fallback for B1 foundation if ServiceId is missing but we need a price.
            // Normally this comes from a dedicated Care Package ServiceType.
            // For now, we use a placeholder lookup or throw if strict price lookup is required.
            // B1 plan: "missing service/active price fails safely."
            throw new InvalidOperationException("ServiceId is required to determine effective price.");
        }

        // 2. Create Domain Entities
        var draft = CarePackageRequest.CreateDraft(
            companyId: companyId,
            customerId: request.CustomerId,
            serviceId: request.ServiceId,
            saleDate: request.SaleDate,
            createdByUserId: userId
        );

        var item = CarePackageRequestItem.Create(
            graveId: request.Item.GraveId,
            cotCountSnapshot: request.Item.CotCount,
            servicePeriodStartDate: request.Item.ServicePeriodStartDate,
            servicePeriodEndDate: request.Item.ServicePeriodStartDate.AddYears(1).AddDays(-1),
            unitPriceSnapshot: unitPriceSnapshot
        );

        draft.AddItem(item);

        if (request.DiscountAmount > 0)
        {
            draft.SetDiscount(request.DiscountAmount, request.DiscountReason);
        }

        draft.EvaluateApprovalRequirement();

        // 3. Save
        db.CarePackageRequests.Add(draft);
        await db.SaveChangesAsync(ct);

        return MapToDto(draft);
    }

    private static CarePackageRequestDto MapToDto(CarePackageRequest entity)
    {
        var dto = new CarePackageRequestDto
        {
            Id = entity.Id,
            CompanyId = entity.CompanyId,
            CustomerId = entity.CustomerId,
            Status = entity.Status,
            RequiresApproval = entity.RequiresApproval,
            WorkflowInstanceId = entity.WorkflowInstanceId,
            ServiceId = entity.ServiceId,
            SaleDate = entity.SaleDate,
            SubtotalAmount = entity.SubtotalAmount,
            DiscountAmount = entity.DiscountAmount,
            DiscountReason = entity.DiscountReason,
            TotalAmount = entity.TotalAmount,
            PaymentTransactionId = entity.PaymentTransactionId,
            PreviousRequestId = entity.PreviousRequestId,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId
        };

        if (entity.Items != null && entity.Items.Any())
        {
            dto.Items = entity.Items.Select(i => new CarePackageRequestItemDto
            {
                Id = i.Id,
                CarePackageRequestId = i.CarePackageRequestId,
                GraveId = i.GraveId,
                CotCountSnapshot = i.CotCountSnapshot,
                ServicePeriodStartDate = i.ServicePeriodStartDate,
                ServicePeriodEndDate = i.ServicePeriodEndDate,
                UnitPriceSnapshot = i.UnitPriceSnapshot,
                LineSubtotal = i.LineSubtotal,
                Notes = i.Notes
            }).ToList();
        }

        return dto;
    }
}
