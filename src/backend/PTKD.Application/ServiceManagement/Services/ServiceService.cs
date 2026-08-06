using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Common.Models;
using PTKD.Application.ServiceManagement.DTOs;
using PTKD.Application.Workflows.DTOs;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.ServiceManagement.Services;

public class ServiceService : IServiceService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly IWorkflowRuntimeService _workflowRuntimeService;

    public ServiceService(
        IOrganizationDbContextFactory dbContextFactory,
        IWorkflowRuntimeService workflowRuntimeService)
    {
        _dbContextFactory = dbContextFactory;
        _workflowRuntimeService = workflowRuntimeService;
    }

    public async Task<PagedResult<ServiceDto>> ListAsync(long companyId, long? customerId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var query = db.Services.AsNoTracking().Where(s => s.CompanyId == companyId);
        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var totalCount = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);

        var serviceTypeIds = items.Select(s => s.ServiceTypeId).Distinct().ToArray();
        var serviceTypes = await db.ServiceTypes.AsNoTracking()
            .Where(st => serviceTypeIds.Contains(st.Id))
            .ToDictionaryAsync(st => st.Id, ct);

        return new PagedResult<ServiceDto>
        {
            Items = items.Select(s => MapToDto(s, serviceTypes.GetValueOrDefault(s.ServiceTypeId))).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();
        var entity = await db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity == null) return null;

        var serviceType = await db.ServiceTypes.AsNoTracking().FirstOrDefaultAsync(st => st.Id == entity.ServiceTypeId, ct);
        return MapToDto(entity, serviceType);
    }

    public async Task<ServiceDto> CreateStandardAsync(CreateServiceRequest request, long userId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var serviceType = await db.ServiceTypes.AsNoTracking().FirstOrDefaultAsync(st => st.Id == request.ServiceTypeId, ct)
            ?? throw new InvalidOperationException("Service type not found.");
        if (!serviceType.IsActive)
            throw new InvalidOperationException("Service type is not active.");

        var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists)
            throw new InvalidOperationException("Customer not found.");

        var companyExists = await db.Companies.AnyAsync(c => c.Id == request.CompanyId, ct);
        if (!companyExists)
            throw new InvalidOperationException("Company not found.");

        var contextExists = await db.CustomerCompanyContexts.AnyAsync(
            ccc => ccc.CustomerId == request.CustomerId && ccc.CompanyId == request.CompanyId, ct);
        if (!contextExists)
            throw new InvalidOperationException("Customer does not have a company context for the specified company.");

        var entity = Service.CreateStandard(
            request.ServiceTypeId,
            request.CustomerId,
            request.CompanyId,
            serviceType.StandardPrice,
            request.ValidFrom,
            request.ValidTo,
            userId);

        db.Services.Add(entity);
        await db.SaveChangesAsync(ct);

        var correlationId = Guid.NewGuid();
        var history = new ServiceHistory(
            entity.Id,
            ServiceHistory.ActionCreated,
            null,
            JsonSerializer.Serialize(new { entity.ServiceTypeId, entity.CustomerId, entity.CompanyId, entity.AppliedPrice, entity.ValidFrom, entity.ValidTo }),
            userId,
            null,
            correlationId);

        db.ServiceHistories.Add(history);
        await db.SaveChangesAsync(ct);

        return MapToDto(entity, serviceType);
    }

    public async Task<ServiceDto> RenewStandardAsync(long serviceId, RenewServiceRequest request, long userId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var existing = await db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct)
            ?? throw new InvalidOperationException("Service not found.");

        if (existing.Status != Service.StatusActive && existing.Status != Service.StatusExpired)
            throw new InvalidOperationException("Service must be in ACTIVE or EXPIRED status to renew.");

        db.Entry(existing).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);

        var serviceType = await db.ServiceTypes.AsNoTracking().FirstOrDefaultAsync(st => st.Id == existing.ServiceTypeId, ct)
            ?? throw new InvalidOperationException("Service type not found.");

        if (serviceType.StandardPrice != existing.StandardPriceSnapshot && serviceType.StandardPrice != serviceType.StandardPrice)
            throw new InvalidOperationException("Standard price has changed. If you need a different price, request a SERVICE_PRICE_OVERRIDE.");

        var beforeData = JsonSerializer.Serialize(new { existing.Id, existing.Status, existing.CycleNumber });

        if (existing.Status == Service.StatusActive)
            existing.Expire();

        var renewal = Service.CreateRenewal(
            existing.ServiceTypeId,
            existing.CustomerId,
            existing.CompanyId,
            serviceType.StandardPrice,
            request.ValidFrom,
            request.ValidTo,
            existing.CycleNumber + 1,
            existing.Id,
            userId);

        db.Services.Add(renewal);
        await db.SaveChangesAsync(ct);

        var correlationId = Guid.NewGuid();
        var history = new ServiceHistory(
            renewal.Id,
            ServiceHistory.ActionRenewed,
            beforeData,
            JsonSerializer.Serialize(new { renewal.ServiceTypeId, renewal.CustomerId, renewal.CompanyId, renewal.AppliedPrice, renewal.CycleNumber, renewal.ValidFrom, renewal.ValidTo }),
            userId,
            null,
            correlationId);

        db.ServiceHistories.Add(history);
        await db.SaveChangesAsync(ct);

        return MapToDto(renewal, serviceType);
    }

    public async Task<long> RequestPriceOverrideAsync(long serviceId, RequestPriceOverrideRequest request, long userId, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct)
            ?? throw new InvalidOperationException("Service not found.");

        if (service.Status != Service.StatusActive)
            throw new InvalidOperationException("Service must be in ACTIVE status to request price override.");

        if (request.RequestedPrice == service.StandardPriceSnapshot)
            throw new InvalidOperationException("Requested price must differ from the standard price.");

        if (request.RequestedPrice <= 0)
            throw new InvalidOperationException("Requested price must be greater than zero.");

        db.Entry(service).Property(e => e.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);

        var serviceType = await db.ServiceTypes.AsNoTracking().FirstOrDefaultAsync(st => st.Id == service.ServiceTypeId, ct);

        var discountAmount = service.StandardPriceSnapshot - request.RequestedPrice;
        var discountPercent = service.StandardPriceSnapshot > 0
            ? Math.Round(discountAmount / service.StandardPriceSnapshot * 100, 2)
            : 0;

        var payloadJson = JsonSerializer.Serialize(new
        {
            service_id = service.Id,
            company_id = service.CompanyId,
            standard_price = service.StandardPriceSnapshot,
            requested_price = request.RequestedPrice,
            discount_amount = discountAmount,
            discount_percent = discountPercent,
            service_type = serviceType?.Code ?? "UNKNOWN",
            reason = request.Reason
        });

        service.SetPendingPriceOverride();
        await db.SaveChangesAsync(ct);

        var workflowRequest = new CreateWorkflowInstanceRequest
        {
            ProcessCode = "SERVICE_PRICE_OVERRIDE",
            BusinessEntityType = "Service",
            BusinessEntityId = service.Id,
            CompanyId = service.CompanyId,
            PayloadJson = payloadJson
        };

        var instance = await _workflowRuntimeService.CreateInstanceAsync(workflowRequest, userId, ct);
        return instance.Id;
    }

    private static ServiceDto MapToDto(Service entity, ServiceType? serviceType)
    {
        return new ServiceDto
        {
            Id = entity.Id,
            ServiceTypeId = entity.ServiceTypeId,
            ServiceTypeCode = serviceType?.Code,
            ServiceTypeName = serviceType?.Name,
            CustomerId = entity.CustomerId,
            CompanyId = entity.CompanyId,
            Status = entity.Status,
            AppliedPrice = entity.AppliedPrice,
            StandardPriceSnapshot = entity.StandardPriceSnapshot,
            IsOverridePrice = entity.IsOverridePrice,
            OverrideApprovalRequestId = entity.OverrideApprovalRequestId,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            CycleNumber = entity.CycleNumber,
            PreviousServiceId = entity.PreviousServiceId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            RowVersion = Convert.ToBase64String(entity.RowVersion)
        };
    }
}
