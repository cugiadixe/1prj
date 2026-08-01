using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.DTOs;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.Customers.Services;

public class CustomerMasterChangeService : ICustomerMasterChangeService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly IWorkflowRuntimeService _workflowRuntimeService;
    private readonly ITransactionalAuditWriter _auditWriter;

    public CustomerMasterChangeService(
        IOrganizationDbContextFactory dbContextFactory,
        IWorkflowRuntimeService workflowRuntimeService,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _workflowRuntimeService = workflowRuntimeService;
        _auditWriter = auditWriter;
    }

    public async Task<CustomerMasterChangeDto> CreateChangeRequestAsync(CreateCustomerMasterChangeRequest request, long actorUserId, long? companyId = null, CancellationToken ct = default)
    {
        var payloadJson = JsonSerializer.Serialize(request);
        byte[] targetRowVersionBytes = Convert.FromBase64String(request.TargetRowVersion);

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var customer = await context.Customers
                .Include(c => c.Profile)
                .FirstOrDefaultAsync(c => c.Id == request.TargetCustomerId, ct);

            if (customer == null)
                throw new BusinessRuleValidationException("CUS_NOT_FOUND", "Customer not found.");

            if (customer.CustomerStatus != "ACTIVE")
                throw new BusinessRuleValidationException("CUS_NOT_ACTIVE", "Can only change active customers.");

            // Duplicate detection logic if CCCD is modified (only read-only check)
            if (!string.IsNullOrWhiteSpace(request.Cccd) && request.Cccd != customer.Profile.Cccd)
            {
                if (await context.Profiles.AnyAsync(p => p.Cccd == request.Cccd && p.IsActive, ct))
                    throw new BusinessRuleValidationException("CUS_DUPLICATE_CCCD", "An active customer with this CCCD already exists.");
            }

            var changeRequest = CustomerChangeRequest.CreateForUpdate(
                "CUSTOMER_MASTER_CHANGE",
                actorUserId,
                payloadJson,
                request.TargetCustomerId,
                targetRowVersionBytes,
                companyId);

            context.CustomerChangeRequests.Add(changeRequest);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_MASTER_CHANGE_PROPOSED",
                EntityType = "CustomerChangeRequest",
                EntityId = changeRequest.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { request.TargetCustomerId, Reason = request.Reason })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            var workflowInstanceRequest = new CreateWorkflowInstanceRequest
            {
                ProcessCode = "CUSTOMER_MASTER_CHANGE",
                BusinessEntityType = "CustomerChangeRequest",
                BusinessEntityId = changeRequest.Id,
                CompanyId = companyId,
                PayloadJson = JsonSerializer.Serialize(new { request.TargetCustomerId, Reason = request.Reason })
            };

            var workflowInstance = await _workflowRuntimeService.CreateInstanceAsync(workflowInstanceRequest, actorUserId, ct);

            await using var linkContext = _dbContextFactory.CreateDbContext();
            var linkStrategy = linkContext.CreateExecutionStrategy();
            await linkStrategy.ExecuteAsync(async () =>
            {
                await using var ctx2 = _dbContextFactory.CreateDbContext();
                var ccr = await ctx2.CustomerChangeRequests.FirstAsync(c => c.Id == changeRequest.Id, ct);
                ccr.SetSubmitted(workflowInstance.Id);
                await ctx2.SaveChangesAsync(ct);
            });

            return MapToDto(changeRequest, workflowInstance.Id);
        });
    }

    public async Task<CustomerMasterChangeDto?> GetChangeRequestByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var ccr = await context.CustomerChangeRequests.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (ccr == null || ccr.ProcessCode != "CUSTOMER_MASTER_CHANGE") return null;
        return MapToDto(ccr, ccr.WorkflowInstanceId);
    }

    public async Task<CustomerMasterChangeDto[]> GetMyChangeRequestsAsync(long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var proposals = await context.CustomerChangeRequests
            .Where(c => c.RequesterId == actorUserId && c.ProcessCode == "CUSTOMER_MASTER_CHANGE")
            .OrderByDescending(c => c.CreatedAt)
            .ToArrayAsync(ct);

        return proposals.Select(p => MapToDto(p, p.WorkflowInstanceId)).ToArray();
    }

    private static CustomerMasterChangeDto MapToDto(CustomerChangeRequest ccr, long? workflowInstanceId)
    {
        CreateCustomerMasterChangeRequest? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<CreateCustomerMasterChangeRequest>(ccr.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { }

        return new CustomerMasterChangeDto
        {
            Id = ccr.Id,
            ProcessCode = ccr.ProcessCode,
            RequesterId = ccr.RequesterId,
            CompanyId = ccr.CompanyId,
            RequestStatus = ccr.RequestStatus,
            WorkflowInstanceId = workflowInstanceId ?? ccr.WorkflowInstanceId,
            TargetCustomerId = ccr.TargetCustomerId,
            TargetRowVersion = ccr.TargetRowVersion != null ? Convert.ToBase64String(ccr.TargetRowVersion) : null,
            CreatedAt = ccr.CreatedAt,
            UpdatedAt = ccr.UpdatedAt,
            RowVersion = ccr.RowVersion != null ? Convert.ToBase64String(ccr.RowVersion) : "",
            Payload = payload
        };
    }
}
