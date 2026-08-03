using System;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.ServiceManagement.Handlers;

public class ServicePriceOverrideExecutionHandler : IWorkflowExecutionHandler
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public ServicePriceOverrideExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public string ProcessCode => "SERVICE_PRICE_OVERRIDE";

    public async Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != "Service")
            throw new InvalidOperationException($"Expected BusinessEntityType 'Service', got '{instance.BusinessEntityType}'.");

        var serviceId = instance.BusinessEntityId;

        await using var dbContext = _dbContextFactory.CreateDbContext();
        await using var transaction = await dbContext.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var service = await dbContext.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct)
            ?? throw new InvalidOperationException($"Service {serviceId} not found.");

        if (service.Status != Service.StatusPendingPriceOverride)
        {
            if (service.IsOverridePrice && service.OverrideApprovalRequestId == instance.Id)
                return;
            throw new InvalidOperationException($"Service {serviceId} is not in PENDING_PRICE_OVERRIDE status.");
        }

        var payload = JsonSerializer.Deserialize<JsonElement>(instance.PayloadJson ?? "{}");
        var requestedPrice = payload.TryGetProperty("requested_price", out var priceEl)
            ? priceEl.GetDecimal()
            : throw new InvalidOperationException("Payload missing 'requested_price'.");

        var beforeData = JsonSerializer.Serialize(new
        {
            service.Id,
            service.Status,
            service.AppliedPrice,
            service.IsOverridePrice
        });

        service.ApplyPriceOverride(requestedPrice, instance.Id);

        var afterData = JsonSerializer.Serialize(new
        {
            service.Id,
            service.Status,
            service.AppliedPrice,
            service.IsOverridePrice,
            service.OverrideApprovalRequestId
        });

        var correlationId = Guid.NewGuid();
        var history = new ServiceHistory(
            service.Id,
            ServiceHistory.ActionPriceOverridden,
            beforeData,
            afterData,
            instance.RequesterId,
            payload.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString() : null,
            correlationId);

        dbContext.ServiceHistories.Add(history);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException($"Concurrency conflict on service {serviceId}.");
        }

        var auditRecord = new SecurityAuditEventRecord
        {
            EventCode = "SERVICE_PRICE_OVERRIDE_EXECUTED",
            EntityType = "Service",
            EntityId = serviceId.ToString(),
            Outcome = "SUCCESS",
            CorrelationId = correlationId,
            ActorUserId = instance.RequesterId,
            CompanyId = service.CompanyId,
            BeforeStateJson = beforeData,
            AfterStateJson = afterData
        };
        auditRecord.ThrowIfContainsSensitiveData();

        await _auditWriter.WriteAsync(
            auditRecord,
            dbContext.GetDbConnection(),
            dbContext.GetCurrentDbTransaction()!,
            ct);

        await transaction.CommitAsync(ct);
    }
}
