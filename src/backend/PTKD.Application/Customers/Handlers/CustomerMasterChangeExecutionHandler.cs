using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.Customers.Handlers;

public class CustomerMasterChangeExecutionHandler : IWorkflowExecutionHandler
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public string ProcessCode => "CUSTOMER_MASTER_CHANGE";

    public CustomerMasterChangeExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != "CustomerChangeRequest")
            throw new InvalidOperationException("Invalid business entity type for this handler.");

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var ccr = await dbContext.CustomerChangeRequests.FirstOrDefaultAsync(c => c.Id == instance.BusinessEntityId, ct);

        if (ccr == null) throw new InvalidOperationException("Customer change request not found.");

        // Idempotency check
        if (ccr.RequestStatus == "EXECUTED")
            return;

        if (ccr.RequestStatus != "SUBMITTED" && ccr.RequestStatus != "APPROVED" && ccr.RequestStatus != "FAILED")
            throw new InvalidOperationException($"Cannot execute request in state {ccr.RequestStatus}.");

        if (ccr.TargetCustomerId == null || ccr.TargetRowVersion == null)
            throw new InvalidOperationException("Target customer ID or row version is missing.");

        var customer = await dbContext.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.Id == ccr.TargetCustomerId, ct);

        if (customer == null)
            throw new InvalidOperationException("Target customer not found.");

        if (!Convert.ToBase64String(customer.RowVersion).Equals(Convert.ToBase64String(ccr.TargetRowVersion)))
            throw new InvalidOperationException("Concurrency conflict: Target customer has been modified since the request was created.");

        CreateCustomerMasterChangeRequest payload;
        try
        {
            payload = JsonSerializer.Deserialize<CreateCustomerMasterChangeRequest>(ccr.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            if (payload == null) throw new Exception();
        }
        catch
        {
            throw new InvalidOperationException("Invalid payload JSON.");
        }

        // Apply changes
        customer.Profile.Update(
            payload.FullName ?? customer.Profile.FullName,
            payload.Cccd ?? customer.Profile.Cccd,
            payload.Dob ?? customer.Profile.Dob,
            payload.DobPartial ?? customer.Profile.DobPartial,
            payload.DobPrecision ?? customer.Profile.DobPrecision,
            payload.Gender ?? customer.Profile.Gender,
            payload.PermanentAddress ?? customer.Profile.PermanentAddress,
            payload.CccdIssueDate ?? customer.Profile.CccdIssueDate,
            payload.CccdIssuePlace ?? customer.Profile.CccdIssuePlace,
            payload.TaxCode ?? customer.Profile.TaxCode,
            payload.Phone ?? customer.Profile.Phone,
            payload.ContactAddress ?? customer.Profile.ContactAddress,
            payload.DeathDateSolar ?? customer.Profile.DeathDateSolar,
            payload.DeathDateLunar ?? customer.Profile.DeathDateLunar,
            payload.DeathPlace ?? customer.Profile.DeathPlace,
            payload.Hometown ?? customer.Profile.Hometown,
            0 // System or fallback actor ID
        );

        ccr.SetExecutedForUpdate();

        try
        {
            await using var transaction = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            await dbContext.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_MASTER_CHANGE_EXECUTED",
                EntityType = "Customer",
                EntityId = customer.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = 0, // Fallback if no context available in handler signature
                AfterStateJson = JsonSerializer.Serialize(new { CustomerId = customer.Id, customer.CustomerCode, RequestId = ccr.Id })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, dbContext.GetDbConnection(), dbContext.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            ccr.SetFailed();
            await dbContext.SaveChangesAsync(ct); // Save failed state outside transaction
            throw new InvalidOperationException("Concurrency conflict during execution.");
        }
    }
}
