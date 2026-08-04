using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;
using PTKD.Application.Security.Audit;
using System.Text.Json;

namespace PTKD.Application.Cards.Handlers;

public class CardReprintExecutionHandler : IWorkflowExecutionHandler
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public string ProcessCode => "CARD_REPRINT";

    public CardReprintExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != "CardReprintRequest")
            throw new InvalidOperationException("Invalid business entity type for this handler.");

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var request = await dbContext.CardReprintRequests.FirstOrDefaultAsync(c => c.Id == instance.BusinessEntityId, ct);

        if (request == null) throw new InvalidOperationException("Card reprint request not found.");

        if (request.Status == CardReprintRequest.StatusApproved)
            return; // Idempotency check

        if (request.Status != CardReprintRequest.StatusPendingApproval)
            throw new InvalidOperationException($"Cannot execute request in state {request.Status}.");

        request.SetApproved();

        try
        {
            await using var transaction = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            await dbContext.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CARD_REPRINT_WORKFLOW_EXECUTED",
                EntityType = "CardReprintRequest",
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
