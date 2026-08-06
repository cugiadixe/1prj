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

namespace PTKD.Application.Customers.Services;

public class CreateCustomerExecutionHandler : IWorkflowExecutionHandler
{
    public string ProcessCode => "CREATE_CUSTOMER";

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public CreateCustomerExecutionHandler(IOrganizationDbContextFactory dbContextFactory, ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var ccr = await context.CustomerChangeRequests
                .FirstOrDefaultAsync(c => c.Id == instance.BusinessEntityId, ct)
                ?? throw new EntityNotFoundException("CCR_NOT_FOUND", "Customer change request not found.");

            if (ccr.CreatedCustomerId.HasValue)
            {
                var existingCustomer = await context.Customers
                    .FirstOrDefaultAsync(c => c.Id == ccr.CreatedCustomerId.Value, ct);
                if (existingCustomer != null)
                {
                    instance.SetExecuted(JsonSerializer.Serialize(new { CustomerId = existingCustomer.Id, Idempotent = true }));
                    ccr.SetExecuted(existingCustomer.Id);
                    var wi = await context.WorkflowInstances.FirstAsync(w => w.Id == instance.Id, ct);
                    wi.SetExecuted(JsonSerializer.Serialize(new { CustomerId = existingCustomer.Id, Idempotent = true }));
                    await context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    return;
                }
            }

            CreateCustomerProposalRequest request;
            try
            {
                request = JsonSerializer.Deserialize<CreateCustomerProposalRequest>(instance.PayloadJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            }
            catch (Exception ex)
            {
                throw new BusinessRuleValidationException("CCR_INVALID_PAYLOAD", $"Cannot deserialize payload: {ex.Message}");
            }

            if (await context.Customers.AnyAsync(c => c.CustomerCode == request.CustomerCode, ct))
                throw new BusinessRuleValidationException("CUS_DUPLICATE_CUSTOMER_CODE", "Customer code already exists.");

            if (!string.IsNullOrWhiteSpace(request.Cccd))
            {
                if (await context.Profiles.AnyAsync(p => p.Cccd == request.Cccd && p.IsActive, ct))
                    throw new BusinessRuleValidationException("CUS_DUPLICATE_CCCD", "An active customer with this CCCD already exists.");
            }

            var profile = new Profile(
                request.FullName, request.Cccd, request.Dob, request.DobPartial, request.DobPrecision,
                request.Gender, request.PermanentAddress, request.CccdIssueDate, request.CccdIssuePlace,
                request.TaxCode, request.Phone, request.ContactAddress,
                request.DeathDateSolar, request.DeathDateLunar, request.DeathPlace, request.Hometown);
            profile.SetCreatedBy(instance.RequesterId);
            context.Profiles.Add(profile);
            await context.SaveChangesAsync(ct);

            var customer = new Customer(request.CustomerCode, profile.Id);
            customer.SetCreatedBy(instance.RequesterId);
            context.Customers.Add(customer);
            await context.SaveChangesAsync(ct);

            if (request.InitialCompanyId.HasValue)
            {
                if (await context.Companies.AnyAsync(c => c.Id == request.InitialCompanyId.Value && c.IsActive, ct))
                {
                    var companyContext = new CustomerCompanyContext(
                        customer.Id, request.InitialCompanyId.Value, request.AssignedStaffId,
                        request.InternalNotes, DateTime.UtcNow);
                    companyContext.SetCreatedBy(instance.RequesterId);
                    context.CustomerCompanyContexts.Add(companyContext);
                    await context.SaveChangesAsync(ct);
                }
            }

            ccr.SetExecuted(customer.Id);

            var wi2 = await context.WorkflowInstances.FirstAsync(w => w.Id == instance.Id, ct);
            wi2.SetExecuted(JsonSerializer.Serialize(new { CustomerId = customer.Id }));

            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_CREATED_VIA_WORKFLOW",
                EntityType = "Customer",
                EntityId = customer.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = instance.CorrelationId,
                ActorUserId = instance.RequesterId,
                AfterStateJson = JsonSerializer.Serialize(new { customer.CustomerCode, profile.FullName, WorkflowInstanceId = instance.Id, ChangeRequestId = ccr.Id })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);
        });
    }
}
