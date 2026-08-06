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

public class CustomerProposalService : ICustomerProposalService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly IWorkflowRuntimeService _workflowRuntimeService;
    private readonly ITransactionalAuditWriter _auditWriter;

    public CustomerProposalService(
        IOrganizationDbContextFactory dbContextFactory,
        IWorkflowRuntimeService workflowRuntimeService,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _workflowRuntimeService = workflowRuntimeService;
        _auditWriter = auditWriter;
    }

    public async Task<CustomerProposalDto> CreateProposalAsync(CreateCustomerProposalRequest request, long actorUserId, CancellationToken ct = default)
    {
        var payloadJson = JsonSerializer.Serialize(request);

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (await context.Customers.AnyAsync(c => c.CustomerCode == request.CustomerCode, ct))
                throw new BusinessRuleValidationException("CUS_DUPLICATE_CUSTOMER_CODE", "Customer code already exists.");

            if (!string.IsNullOrWhiteSpace(request.Cccd))
            {
                if (await context.Profiles.AnyAsync(p => p.Cccd == request.Cccd && p.IsActive, ct))
                    throw new BusinessRuleValidationException("CUS_DUPLICATE_CCCD", "An active customer with this CCCD already exists.");
            }

            var changeRequest = new CustomerChangeRequest("CREATE_CUSTOMER", actorUserId, payloadJson, request.InitialCompanyId);
            context.CustomerChangeRequests.Add(changeRequest);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_PROPOSAL_CREATED",
                EntityType = "CustomerChangeRequest",
                EntityId = changeRequest.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { request.CustomerCode, request.FullName, request.InitialCompanyId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            var workflowInstanceRequest = new CreateWorkflowInstanceRequest
            {
                ProcessCode = "CREATE_CUSTOMER",
                BusinessEntityType = "CustomerChangeRequest",
                BusinessEntityId = changeRequest.Id,
                CompanyId = request.InitialCompanyId,
                PayloadJson = payloadJson
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

    public async Task<CustomerProposalDto?> GetProposalByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var ccr = await context.CustomerChangeRequests.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (ccr == null) return null;
        return MapToDto(ccr, ccr.WorkflowInstanceId);
    }

    public async Task<CustomerProposalDto[]> GetMyProposalsAsync(long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var proposals = await context.CustomerChangeRequests
            .Where(c => c.RequesterId == actorUserId)
            .OrderByDescending(c => c.CreatedAt)
            .ToArrayAsync(ct);

        return proposals.Select(p => MapToDto(p, p.WorkflowInstanceId)).ToArray();
    }

    private static CustomerProposalDto MapToDto(CustomerChangeRequest ccr, long? workflowInstanceId)
    {
        CustomerProposalSummaryDto? summary = null;
        try
        {
            using var doc = JsonDocument.Parse(ccr.PayloadJson);
            var root = doc.RootElement;
            summary = new CustomerProposalSummaryDto
            {
                CustomerCode = root.TryGetProperty("CustomerCode", out var cc) ? cc.GetString() ?? ""
                    : root.TryGetProperty("customerCode", out var cc2) ? cc2.GetString() ?? "" : "",
                FullName = root.TryGetProperty("FullName", out var fn) ? fn.GetString() ?? ""
                    : root.TryGetProperty("fullName", out var fn2) ? fn2.GetString() ?? "" : "",
                CompanyId = root.TryGetProperty("InitialCompanyId", out var cid) && cid.ValueKind == JsonValueKind.Number ? cid.GetInt64()
                    : root.TryGetProperty("initialCompanyId", out var cid2) && cid2.ValueKind == JsonValueKind.Number ? cid2.GetInt64() : null
            };
        }
        catch { }

        return new CustomerProposalDto
        {
            Id = ccr.Id,
            ProcessCode = ccr.ProcessCode,
            RequesterId = ccr.RequesterId,
            CompanyId = ccr.CompanyId,
            RequestStatus = ccr.RequestStatus,
            WorkflowInstanceId = workflowInstanceId ?? ccr.WorkflowInstanceId,
            CreatedCustomerId = ccr.CreatedCustomerId,
            CreatedAt = ccr.CreatedAt,
            UpdatedAt = ccr.UpdatedAt,
            RowVersion = Convert.ToBase64String(ccr.RowVersion),
            Summary = summary
        };
    }
}
