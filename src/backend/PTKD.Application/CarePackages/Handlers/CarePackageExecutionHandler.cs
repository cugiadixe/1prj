using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;
using PTKD.Application.Security.Audit;
using System.Text.Json;

namespace PTKD.Application.CarePackages.Handlers;

public class CarePackageExecutionHandler : IWorkflowExecutionHandler
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public string ProcessCode => "SELL_CARE_PACKAGE";

    public CarePackageExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != "CarePackageRequest")
            throw new InvalidOperationException("Invalid business entity type for this handler.");

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CarePackageRequests.FirstOrDefaultAsync(c => c.Id == instance.BusinessEntityId, ct);

        if (request == null) throw new InvalidOperationException("Care package request not found.");

        if (request.Status == CarePackageRequest.StatusApproved || request.Status == CarePackageRequest.StatusPaymentEligible)
            return; // Idempotency check

        if (request.Status != CarePackageRequest.StatusPendingApproval)
            throw new InvalidOperationException($"Cannot execute request in state {request.Status}.");

        request.SetApproved();
        // Since B2 requirements say "payment eligibility guard: no approval required and valid configured price, or approval required and approved",
        // we can set it to PaymentEligible after it's approved.
        request.SetPaymentEligible();

        try
        {
            await using var transaction = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            await dbContext.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "SELL_CARE_PACKAGE_WORKFLOW_EXECUTED",
                EntityType = "CarePackageRequest",
                EntityId = request.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = 0, // Fallback actor
                AfterStateJson = JsonSerializer.Serialize(new { RequestId = request.Id, Status = request.Status })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, dbContext.GetDbConnection(), dbContext.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Concurrency conflict during execution.");
        }
    }
}
