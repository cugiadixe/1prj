using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.DTOs;
using PTKD.Domain.Entities;
using PTKD.Domain.ValueObjects;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Application.Workflows.Services;

public class WorkflowRuntimeService : IWorkflowRuntimeService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;
    private readonly IApproverResolver _approverResolver;
    private readonly IWorkflowExecutionHandlerFactory _executionHandlerFactory;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public WorkflowRuntimeService(IOrganizationDbContextFactory dbContextFactory, ITransactionalAuditWriter auditWriter, IApproverResolver approverResolver, IWorkflowExecutionHandlerFactory executionHandlerFactory, IPermissionEvaluator permissionEvaluator)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
        _approverResolver = approverResolver;
        _executionHandlerFactory = executionHandlerFactory;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<WorkflowInstanceDto> CreateInstanceAsync(CreateWorkflowInstanceRequest request, long requesterId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var now = DateTime.UtcNow;

            var binding = await context.WorkflowBindings
                .Where(b => b.IsActive
                    && b.ProcessCode == request.ProcessCode
                    && b.EffectiveFrom <= now
                    && (b.EffectiveTo == null || b.EffectiveTo > now))
                .Where(b => (b.ScopeType == "COMPANY" && b.CompanyId == request.CompanyId)
                    || b.ScopeType == "GLOBAL")
                .OrderByDescending(b => b.ScopeType == "COMPANY" ? 1 : 0)
                .ThenByDescending(b => b.Priority)
                .FirstOrDefaultAsync(ct);

            if (binding == null)
                throw new BusinessRuleValidationException("WF_NO_VALID_BINDING", "No active workflow binding found for this process and scope.");

            var version = await context.WorkflowDefinitionVersions
                .Include(v => v.Steps).ThenInclude(s => s.ApproverRules)
                .FirstOrDefaultAsync(v => v.Id == binding.WorkflowVersionId, ct)
                ?? throw new BusinessRuleValidationException("WF_VERSION_NOT_FOUND", "Bound workflow version not found.");

            if (!version.IsActive)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_ACTIVE", "Bound workflow version is not active.");

            var snapshotJson = JsonSerializer.Serialize(new
            {
                version.Id,
                version.VersionNumber,
                version.WorkflowDefinitionId,
                Steps = version.Steps.OrderBy(s => s.StepOrder).Select(s => new
                {
                    s.Id, s.StepOrder, s.StepName, s.IsRequired,
                    ApproverRules = s.ApproverRules.Select(r => new { r.ApproverSourceType, r.ApproverSourceValue, r.Priority })
                })
            });

            var payloadHash = ComputeHash(request.PayloadJson);

            var instance = new WorkflowInstance(
                version.Id, binding.Id, request.ProcessCode, requesterId,
                request.BusinessEntityType, request.BusinessEntityId,
                snapshotJson, request.PayloadJson, payloadHash,
                request.CompanyId, request.BeforeDataJson);

            context.WorkflowInstances.Add(instance);
            await context.SaveChangesAsync(ct);

            await CreateInstanceStepsAsync(context, instance, version.Steps, requesterId, request.CompanyId, ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_INSTANCE_CREATED",
                EntityType = "WorkflowInstance",
                EntityId = instance.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = instance.CorrelationId,
                ActorUserId = requesterId,
                AfterStateJson = JsonSerializer.Serialize(new { instance.ProcessCode, instance.BusinessEntityType, instance.BusinessEntityId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return await LoadInstanceDtoAsync(context, instance.Id, ct);
        });
    }

    public async Task<WorkflowInstanceDto?> GetInstanceByIdAsync(long instanceId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        if (!await context.WorkflowInstances.AsNoTracking().AnyAsync(i => i.Id == instanceId, ct))
            return null;
        return await LoadInstanceDtoAsync(context, instanceId, ct);
    }

    public async Task<MyApprovalItemDto[]> GetMyPendingApprovalsAsync(long userId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        return await context.WorkflowInstanceStepAssignees
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Join(
                context.WorkflowInstanceSteps.Where(s => s.StepStatus == "PENDING"),
                a => a.WorkflowInstanceStepId, s => s.Id, (a, s) => s)
            .Join(
                context.WorkflowInstances.Where(i => i.InstanceStatus == "PENDING_APPROVAL"),
                s => s.WorkflowInstanceId, i => i.Id,
                (s, i) => new MyApprovalItemDto
                {
                    InstanceId = i.Id,
                    StepId = s.Id,
                    ProcessCode = i.ProcessCode,
                    BusinessEntityType = i.BusinessEntityType,
                    BusinessEntityId = i.BusinessEntityId,
                    StepName = s.StepName,
                    InstanceStatus = i.InstanceStatus,
                    AssignedAt = s.AssignedAt
                })
            .OrderByDescending(a => a.AssignedAt)
            .ToArrayAsync(ct);
    }

    public async Task<WorkflowInstanceDto> ApproveStepAsync(long instanceId, long stepId, ApprovalActionRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var instance = await context.WorkflowInstances
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_INSTANCE_NOT_FOUND", "Workflow instance not found.");

            if (instance.InstanceStatus != "PENDING_APPROVAL")
                throw new BusinessRuleValidationException("WF_INSTANCE_NOT_PENDING", "Instance is not pending approval.");

            var step = await context.WorkflowInstanceSteps
                .Include(s => s.Assignees)
                .FirstOrDefaultAsync(s => s.Id == stepId && s.WorkflowInstanceId == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_STEP_NOT_FOUND", "Instance step not found.");

            if (step.StepStatus != "PENDING")
                throw new BusinessRuleValidationException("WF_STEP_NOT_PENDING", "Step is not pending.");

            if (!step.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The step has been modified by another user.");

            if (!step.Assignees.Any(a => a.UserId == actorUserId))
                throw new BusinessRuleValidationException("WF_NOT_ASSIGNEE", "You are not an assignee for this step.");

            if (actorUserId == instance.RequesterId)
                throw new BusinessRuleValidationException("WF_REQUESTER_IS_APPROVER", "Requester cannot approve their own request.");

            step.SetApproved(actorUserId);

            var action = new WorkflowAction(stepId, instanceId, "APPROVE", actorUserId, request.Reason, request.Comment);
            context.WorkflowActions.Add(action);
            await context.SaveChangesAsync(ct);

            var nextStep = await context.WorkflowInstanceSteps
                .Where(s => s.WorkflowInstanceId == instanceId && s.RoundNo == instance.RoundNo && s.StepStatus == "WAITING")
                .OrderBy(s => s.StepOrder)
                .FirstOrDefaultAsync(ct);

            if (nextStep != null)
            {
                nextStep.SetPending();
                await context.SaveChangesAsync(ct);
            }
            else
            {
                instance.SetPendingExecution();
                await context.SaveChangesAsync(ct);
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "APPROVAL_ACTION_TAKEN",
                EntityType = "WorkflowInstanceStep",
                EntityId = stepId.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = instance.CorrelationId,
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { ActionType = "APPROVE", StepId = stepId, InstanceId = instanceId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            if (instance.InstanceStatus == "PENDING_EXECUTION")
            {
                var handler = _executionHandlerFactory.GetHandler(instance.ProcessCode);
                if (handler != null)
                {
                    try
                    {
                        await handler.ExecuteAsync(instance, ct);
                    }
                    catch (Exception)
                    {
                        await using var failCtx = _dbContextFactory.CreateDbContext();
                        var failStrategy = failCtx.CreateExecutionStrategy();
                        await failStrategy.ExecuteAsync(async () =>
                        {
                            await using var ctx = _dbContextFactory.CreateDbContext();
                            var wi = await ctx.WorkflowInstances.FirstAsync(w => w.Id == instanceId, ct);
                            wi.SetFailed();
                            await ctx.SaveChangesAsync(ct);
                        });
                        throw;
                    }
                }
            }

            return await LoadInstanceDtoAsync(context, instanceId, ct);
        });
    }

    public async Task<WorkflowInstanceDto> ReturnStepAsync(long instanceId, long stepId, ApprovalActionRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BusinessRuleValidationException("WF_REASON_REQUIRED", "Reason is required for return action.");

        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var instance = await context.WorkflowInstances
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_INSTANCE_NOT_FOUND", "Workflow instance not found.");

            if (instance.InstanceStatus != "PENDING_APPROVAL")
                throw new BusinessRuleValidationException("WF_INSTANCE_NOT_PENDING", "Instance is not pending approval.");

            var step = await context.WorkflowInstanceSteps
                .Include(s => s.Assignees)
                .FirstOrDefaultAsync(s => s.Id == stepId && s.WorkflowInstanceId == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_STEP_NOT_FOUND", "Instance step not found.");

            if (step.StepStatus != "PENDING")
                throw new BusinessRuleValidationException("WF_STEP_NOT_PENDING", "Step is not pending.");

            if (!step.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The step has been modified by another user.");

            if (!step.Assignees.Any(a => a.UserId == actorUserId))
                throw new BusinessRuleValidationException("WF_NOT_ASSIGNEE", "You are not an assignee for this step.");

            step.SetReturned(actorUserId);

            var futureSteps = await context.WorkflowInstanceSteps
                .Where(s => s.WorkflowInstanceId == instanceId && s.RoundNo == instance.RoundNo && s.StepStatus == "WAITING")
                .ToListAsync(ct);
            foreach (var futureStep in futureSteps)
                futureStep.SetCancelled();

            instance.SetReturned();

            var action = new WorkflowAction(stepId, instanceId, "RETURN", actorUserId, request.Reason, request.Comment);
            context.WorkflowActions.Add(action);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "APPROVAL_ACTION_TAKEN",
                EntityType = "WorkflowInstanceStep",
                EntityId = stepId.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = instance.CorrelationId,
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { ActionType = "RETURN", StepId = stepId, InstanceId = instanceId, request.Reason })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return await LoadInstanceDtoAsync(context, instanceId, ct);
        });
    }

    public async Task<WorkflowInstanceDto> ResubmitInstanceAsync(long instanceId, string targetVersion, long requesterId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(targetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var instance = await context.WorkflowInstances
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_INSTANCE_NOT_FOUND", "Workflow instance not found.");

            if (instance.InstanceStatus != "RETURNED")
                throw new BusinessRuleValidationException("WF_INSTANCE_NOT_RETURNED", "Only RETURNED instances can be resubmitted.");

            if (instance.RequesterId != requesterId)
                throw new BusinessRuleValidationException("WF_NOT_REQUESTER", "Only the original requester can resubmit.");

            if (!instance.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The instance has been modified by another user.");

            instance.Resubmit();

            var version = await context.WorkflowDefinitionVersions
                .Include(v => v.Steps).ThenInclude(s => s.ApproverRules)
                .FirstOrDefaultAsync(v => v.Id == instance.WorkflowVersionId, ct)
                ?? throw new BusinessRuleValidationException("WF_VERSION_NOT_FOUND", "Original workflow version not found.");

            await context.SaveChangesAsync(ct);
            await CreateInstanceStepsAsync(context, instance, version.Steps, requesterId, instance.CompanyId, ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_INSTANCE_RESUBMITTED",
                EntityType = "WorkflowInstance",
                EntityId = instance.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = instance.CorrelationId,
                ActorUserId = requesterId,
                AfterStateJson = JsonSerializer.Serialize(new { instance.RoundNo })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return await LoadInstanceDtoAsync(context, instance.Id, ct);
        });
    }

    public async Task<WorkflowInstanceDto> WithdrawInstanceAsync(long instanceId, string targetVersion, long requesterId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(targetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var instance = await context.WorkflowInstances
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_INSTANCE_NOT_FOUND", "Workflow instance not found.");

            if (instance.InstanceStatus != "PENDING_APPROVAL" && instance.InstanceStatus != "RETURNED")
                throw new BusinessRuleValidationException("WF_CANNOT_WITHDRAW", "Instance can only be withdrawn when PENDING_APPROVAL or RETURNED.");

            if (instance.RequesterId != requesterId)
                throw new BusinessRuleValidationException("WF_NOT_REQUESTER", "Only the original requester can withdraw.");

            if (!instance.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The instance has been modified by another user.");

            var pendingSteps = await context.WorkflowInstanceSteps
                .Where(s => s.WorkflowInstanceId == instanceId && (s.StepStatus == "PENDING" || s.StepStatus == "WAITING"))
                .ToListAsync(ct);
            foreach (var step in pendingSteps)
                step.SetCancelled();

            instance.SetWithdrawn();
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_INSTANCE_WITHDRAWN",
                EntityType = "WorkflowInstance",
                EntityId = instance.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = instance.CorrelationId,
                ActorUserId = requesterId
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return await LoadInstanceDtoAsync(context, instance.Id, ct);
        });
    }

    public async Task<WorkflowInstanceDto> ReassignStepAsync(long instanceId, long stepId, ReassignStepRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var instance = await context.WorkflowInstances
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_INSTANCE_NOT_FOUND", "Workflow instance not found.");

            if (instance.InstanceStatus != "PENDING_APPROVAL")
                throw new BusinessRuleValidationException("WF_INSTANCE_NOT_PENDING", "Instance is not pending approval.");

            var step = await context.WorkflowInstanceSteps
                .Include(s => s.Assignees)
                .FirstOrDefaultAsync(s => s.Id == stepId && s.WorkflowInstanceId == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_STEP_NOT_FOUND", "Instance step not found.");

            if (step.StepStatus != "PENDING")
                throw new BusinessRuleValidationException("WF_STEP_NOT_PENDING", "Step is not pending.");

            if (!step.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The step has been modified by another user.");

            if (request.NewAssigneeUserId == instance.RequesterId)
                throw new BusinessRuleValidationException("WF_REQUESTER_IS_APPROVER", "Cannot reassign to the requester.");

            if (!await context.Users.AnyAsync(u => u.Id == request.NewAssigneeUserId && u.AccountStatus == "ACTIVE", ct))
                throw new EntityNotFoundException("WF_USER_NOT_FOUND", "Target user not found or inactive.");

            if (!step.Assignees.Any(a => a.UserId == request.NewAssigneeUserId))
            {
                var assignee = new WorkflowInstanceStepAssignee(stepId, request.NewAssigneeUserId, "REASSIGN");
                context.WorkflowInstanceStepAssignees.Add(assignee);
            }

            var action = new WorkflowAction(stepId, instanceId, "REASSIGN", actorUserId, request.Reason);
            context.WorkflowActions.Add(action);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "APPROVAL_STEP_REASSIGNED",
                EntityType = "WorkflowInstanceStep",
                EntityId = stepId.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = instance.CorrelationId,
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { request.NewAssigneeUserId, request.Reason })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return await LoadInstanceDtoAsync(context, instance.Id, ct);
        });
    }

    public async Task<WorkflowInstanceDto[]> GetMyRequestsAsync(long requesterId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var instances = await context.WorkflowInstances
            .AsNoTracking()
            .Where(i => i.RequesterId == requesterId)
            .OrderByDescending(i => i.CreatedAt)
            .ToArrayAsync(ct);

        var instanceIds = instances.Select(i => i.Id).ToArray();
        var steps = await context.WorkflowInstanceSteps
            .AsNoTracking()
            .Include(s => s.Assignees)
            .Where(s => instanceIds.Contains(s.WorkflowInstanceId))
            .ToArrayAsync(ct);

        return instances.Select(instance => new WorkflowInstanceDto
        {
            Id = instance.Id,
            WorkflowVersionId = instance.WorkflowVersionId,
            ProcessCode = instance.ProcessCode,
            CompanyId = instance.CompanyId,
            RequesterId = instance.RequesterId,
            BusinessEntityType = instance.BusinessEntityType,
            BusinessEntityId = instance.BusinessEntityId,
            InstanceStatus = instance.InstanceStatus,
            RoundNo = instance.RoundNo,
            RowVersion = Convert.ToBase64String(instance.RowVersion),
            CreatedAt = instance.CreatedAt,
            UpdatedAt = instance.UpdatedAt,
            Steps = steps
                .Where(s => s.WorkflowInstanceId == instance.Id)
                .OrderBy(s => s.RoundNo).ThenBy(s => s.StepOrder)
                .Select(s => new WorkflowInstanceStepDto
                {
                    Id = s.Id,
                    StepOrder = s.StepOrder,
                    StepName = s.StepName,
                    RoundNo = s.RoundNo,
                    StepStatus = s.StepStatus,
                    AssignedAt = s.AssignedAt,
                    CompletedAt = s.CompletedAt,
                    CompletedBy = s.CompletedBy,
                    RowVersion = Convert.ToBase64String(s.RowVersion),
                    Assignees = s.Assignees.Select(a => new WorkflowInstanceStepAssigneeDto
                    {
                        UserId = a.UserId,
                        ApproverSourceType = a.ApproverSourceType
                    }).ToArray()
                }).ToArray()
        }).ToArray();
    }

    public async Task<WorkflowActionDto[]> GetInstanceActionsAsync(long instanceId, long userId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        var instance = await context.WorkflowInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
            ?? throw new EntityNotFoundException("WF_INSTANCE_NOT_FOUND", "Workflow instance not found.");

        if (instance.RequesterId != userId)
        {
            var isAssignee = await context.WorkflowInstanceStepAssignees
                .AsNoTracking()
                .AnyAsync(a => a.UserId == userId && a.Step.WorkflowInstanceId == instanceId, ct);

            if (!isAssignee)
            {
                var canView = await _permissionEvaluator.EvaluateAsync(userId, "WORKFLOW_VIEW", instance.CompanyId, ct)
                    || await _permissionEvaluator.EvaluateAsync(userId, "WORKFLOW_VIEW", null, ct);

                if (!canView)
                    throw new BusinessRuleValidationException("WF_ACTION_HISTORY_UNAUTHORIZED", "You do not have permission to view this instance's action history.");
            }
        }

        return await context.WorkflowActions
            .AsNoTracking()
            .Where(a => a.WorkflowInstanceId == instanceId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new WorkflowActionDto
            {
                Id = a.Id,
                WorkflowInstanceStepId = a.WorkflowInstanceStepId,
                WorkflowInstanceId = a.WorkflowInstanceId,
                ActionType = a.ActionType,
                ActedBy = a.ActedBy,
                OnBehalfOf = a.OnBehalfOf,
                Reason = a.Reason,
                Comment = a.Comment,
                CreatedAt = a.CreatedAt
            })
            .ToArrayAsync(ct);
    }

    public async Task<WorkflowInstanceDto> RejectStepAsync(long instanceId, long stepId, ApprovalActionRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BusinessRuleValidationException("WF_REASON_REQUIRED", "Reason is required for reject action.");

        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var instance = await context.WorkflowInstances
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_INSTANCE_NOT_FOUND", "Workflow instance not found.");

            if (instance.InstanceStatus != "PENDING_APPROVAL")
                throw new BusinessRuleValidationException("WF_INSTANCE_NOT_PENDING", "Instance is not pending approval.");

            var step = await context.WorkflowInstanceSteps
                .Include(s => s.Assignees)
                .FirstOrDefaultAsync(s => s.Id == stepId && s.WorkflowInstanceId == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_STEP_NOT_FOUND", "Instance step not found.");

            if (step.StepStatus != "PENDING")
                throw new BusinessRuleValidationException("WF_STEP_NOT_PENDING", "Step is not pending.");

            if (!step.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The step has been modified by another user.");

            if (!step.Assignees.Any(a => a.UserId == actorUserId))
                throw new BusinessRuleValidationException("WF_NOT_ASSIGNEE", "You are not an assignee for this step.");

            step.SetReturned(actorUserId); // Reusing SetReturned for step status, as per original enum (CANCELLED or RETURNED usually, wait step status has APPROVED, RETURNED, CANCELLED. We will use CANCELLED or RETURNED). Let's use RETURNED for the step to indicate negative completion.

            var futureSteps = await context.WorkflowInstanceSteps
                .Where(s => s.WorkflowInstanceId == instanceId && s.RoundNo == instance.RoundNo && s.StepStatus == "WAITING")
                .ToListAsync(ct);
            foreach (var futureStep in futureSteps)
                futureStep.SetCancelled();

            instance.SetRejected();

            var action = new WorkflowAction(stepId, instanceId, "REJECT", actorUserId, request.Reason, request.Comment);
            context.WorkflowActions.Add(action);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "APPROVAL_ACTION_TAKEN",
                EntityType = "WorkflowInstanceStep",
                EntityId = stepId.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = instance.CorrelationId,
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { ActionType = "REJECT", StepId = stepId, InstanceId = instanceId, request.Reason })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return await LoadInstanceDtoAsync(context, instanceId, ct);
        });
    }

    public async Task<WorkflowInstanceDto> RetryExecutionAsync(long instanceId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var instance = await context.WorkflowInstances
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
                ?? throw new EntityNotFoundException("WF_INSTANCE_NOT_FOUND", "Workflow instance not found.");

            if (instance.InstanceStatus != "FAILED")
                throw new BusinessRuleValidationException("WF_INSTANCE_NOT_FAILED", "Only FAILED instances can be retried.");

            instance.SetPendingExecution();

            // We need a dummy step ID for the action or just the last step
            var lastStep = await context.WorkflowInstanceSteps
                .Where(s => s.WorkflowInstanceId == instanceId)
                .OrderByDescending(s => s.RoundNo).ThenByDescending(s => s.StepOrder)
                .FirstOrDefaultAsync(ct);

            var action = new WorkflowAction(lastStep?.Id ?? 0, instanceId, "RETRY", actorUserId, "Manual execution retry initiated.");
            context.WorkflowActions.Add(action);

            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_EXECUTION_RETRIED",
                EntityType = "WorkflowInstance",
                EntityId = instanceId.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = instance.CorrelationId,
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { instance.InstanceStatus })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            // Trigger execution
            var handler = _executionHandlerFactory.GetHandler(instance.ProcessCode);
            if (handler != null)
            {
                try
                {
                    await handler.ExecuteAsync(instance, ct);
                }
                catch (Exception)
                {
                    await using var failCtx = _dbContextFactory.CreateDbContext();
                    var failStrategy = failCtx.CreateExecutionStrategy();
                    await failStrategy.ExecuteAsync(async () =>
                    {
                        await using var ctx = _dbContextFactory.CreateDbContext();
                        var wi = await ctx.WorkflowInstances.FirstAsync(w => w.Id == instanceId, ct);
                        wi.SetFailed();
                        await ctx.SaveChangesAsync(ct);
                    });
                    throw; // Allow caller to see failure
                }
            }

            return await LoadInstanceDtoAsync(context, instanceId, ct);
        });
    }

    private async Task CreateInstanceStepsAsync(IOrganizationDbContext context, WorkflowInstance instance, System.Collections.Generic.ICollection<WorkflowStep> templateSteps, long requesterId, long? companyId, CancellationToken ct)
    {
        var sortedSteps = templateSteps.OrderBy(s => s.StepOrder).ToList();
        for (int i = 0; i < sortedSteps.Count; i++)
        {
            var templateStep = sortedSteps[i];
            var stepStatus = i == 0 ? "PENDING" : "WAITING";

            var instanceStep = new WorkflowInstanceStep(
                instance.Id, templateStep.Id, templateStep.StepOrder,
                templateStep.StepName, instance.RoundNo, stepStatus);

            if (i == 0)
                instanceStep.SetPending();

            context.WorkflowInstanceSteps.Add(instanceStep);
            await context.SaveChangesAsync(ct);

            foreach (var rule in templateStep.ApproverRules)
            {
                var resolvedUserIds = await _approverResolver.ResolveApproversAsync(
                    rule.ApproverSourceType, rule.ApproverSourceValue, requesterId, companyId, ct);

                foreach (var userId in resolvedUserIds)
                {
                    var assignee = new WorkflowInstanceStepAssignee(instanceStep.Id, userId, rule.ApproverSourceType);
                    context.WorkflowInstanceStepAssignees.Add(assignee);
                }
            }
            await context.SaveChangesAsync(ct);

            var assigneeCount = await context.WorkflowInstanceStepAssignees
                .CountAsync(a => a.WorkflowInstanceStepId == instanceStep.Id, ct);
            if (assigneeCount == 0)
                throw new BusinessRuleValidationException("WF_NO_ASSIGNEE_FOR_STEP", $"No valid approvers resolved for step '{templateStep.StepName}' after excluding requester.");
        }
    }

    private static async Task<WorkflowInstanceDto> LoadInstanceDtoAsync(IOrganizationDbContext context, long instanceId, CancellationToken ct)
    {
        var instance = await context.WorkflowInstances
            .AsNoTracking()
            .FirstAsync(i => i.Id == instanceId, ct);

        var steps = await context.WorkflowInstanceSteps
            .AsNoTracking()
            .Include(s => s.Assignees)
            .Where(s => s.WorkflowInstanceId == instanceId)
            .OrderBy(s => s.RoundNo).ThenBy(s => s.StepOrder)
            .ToArrayAsync(ct);

        return new WorkflowInstanceDto
        {
            Id = instance.Id,
            WorkflowVersionId = instance.WorkflowVersionId,
            ProcessCode = instance.ProcessCode,
            CompanyId = instance.CompanyId,
            RequesterId = instance.RequesterId,
            BusinessEntityType = instance.BusinessEntityType,
            BusinessEntityId = instance.BusinessEntityId,
            InstanceStatus = instance.InstanceStatus,
            RoundNo = instance.RoundNo,
            RowVersion = Convert.ToBase64String(instance.RowVersion),
            CreatedAt = instance.CreatedAt,
            UpdatedAt = instance.UpdatedAt,
            Steps = steps.Select(s => new WorkflowInstanceStepDto
            {
                Id = s.Id,
                StepOrder = s.StepOrder,
                StepName = s.StepName,
                RoundNo = s.RoundNo,
                StepStatus = s.StepStatus,
                AssignedAt = s.AssignedAt,
                CompletedAt = s.CompletedAt,
                CompletedBy = s.CompletedBy,
                RowVersion = Convert.ToBase64String(s.RowVersion),
                Assignees = s.Assignees.Select(a => new WorkflowInstanceStepAssigneeDto
                {
                    UserId = a.UserId,
                    ApproverSourceType = a.ApproverSourceType
                }).ToArray()
            }).ToArray()
        };
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
