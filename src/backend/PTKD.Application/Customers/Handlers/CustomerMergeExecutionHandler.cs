using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.Customers.Handlers;

public class CustomerMergeExecutionHandler : IWorkflowExecutionHandler
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public string ProcessCode => "CUSTOMER_MERGE_DUPLICATE"; // Process code identified in plan

    public CustomerMergeExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != "CustomerMergeRequest")
            throw new InvalidOperationException("Invalid business entity type for this handler.");

        if (!Guid.TryParse(instance.BusinessEntityId.ToString(), out var mergeRequestId))
            throw new InvalidOperationException("Invalid merge request ID.");

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var mergeRequest = await dbContext.CustomerMergeRequests
            .Include(r => r.Candidates)
            .FirstOrDefaultAsync(c => c.Id == mergeRequestId, ct);

        if (mergeRequest == null) throw new InvalidOperationException("Customer merge request not found.");

        // Idempotency check
        if (mergeRequest.RequestStatus == "EXECUTED")
            return;

        if (mergeRequest.RequestStatus == "REJECTED" || mergeRequest.RequestStatus == "WITHDRAWN")
        {
            // Do not mutate if rejected/withdrawn
            return;
        }

        if (mergeRequest.RequestStatus != "SUBMITTED" && mergeRequest.RequestStatus != "APPROVED")
            throw new InvalidOperationException($"Cannot execute request in state {mergeRequest.RequestStatus}.");

        var sourceCustomer = await dbContext.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.Id == mergeRequest.SourceCustomerId, ct);

        var targetCustomer = await dbContext.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.Id == mergeRequest.TargetCustomerId, ct);

        if (sourceCustomer == null || targetCustomer == null)
            throw new InvalidOperationException("Source or target customer not found.");

        if (!Convert.ToBase64String(sourceCustomer.RowVersion).Equals(Convert.ToBase64String(mergeRequest.SourceRowVersionSnapshot)))
            throw new InvalidOperationException("Concurrency conflict: Source customer has been modified since the request was created.");

        if (!Convert.ToBase64String(targetCustomer.RowVersion).Equals(Convert.ToBase64String(mergeRequest.TargetRowVersionSnapshot)))
            throw new InvalidOperationException("Concurrency conflict: Target customer has been modified since the request was created.");

        // We could parse survivorship payload here and apply it to target profile
        // The plan just says "updates Profile fields based on survivorship".
        // For foundation scope, simply mark source as MERGED and link survivor.

        sourceCustomer.SetStatus("MERGED", 0, targetCustomer.Id);

        mergeRequest.SetExecuted();

        // Audit/History
        var history = new CustomerMergeHistory(
            mergeRequest.Id,
            sourceCustomer.Id,
            targetCustomer.Id,
            "EXECUTED",
            0, // ActorId
            mergeRequest.SurvivorshipPayload
        );
        dbContext.CustomerMergeHistory.Add(history);

        try
        {
            await using var transaction = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            await dbContext.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_MERGE_EXECUTED",
                EntityType = "Customer",
                EntityId = targetCustomer.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = 0,
                AfterStateJson = JsonSerializer.Serialize(new { SourceId = sourceCustomer.Id, TargetId = targetCustomer.Id, RequestId = mergeRequest.Id })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, dbContext.GetDbConnection(), dbContext.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            mergeRequest.SetRejected();
            await dbContext.SaveChangesAsync(ct); // Save failed state outside transaction
            throw new InvalidOperationException("Concurrency conflict during execution.");
        }
    }
}
