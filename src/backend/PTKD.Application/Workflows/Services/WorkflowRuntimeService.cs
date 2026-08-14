using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Common.Models;
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
        // CHẶN SỚM: không có bộ xử lý thì duyệt xong cũng không có gì chạy, hồ sơ sẽ kẹt
        // vĩnh viễn ở PENDING_EXECUTION mà không báo lỗi. Thà chặn ngay lúc tạo.
        if (!_executionHandlerFactory.HasHandler(request.ProcessCode))
            throw new BusinessRuleValidationException(
                "WF_NO_EXECUTION_HANDLER",
                $"Quy trình '{request.ProcessCode}' chưa có bộ xử lý thực thi nên chưa thể sử dụng. Vui lòng báo bộ phận CNTT.");

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var now = DateTime.UtcNow;

            var candidateBindings = await context.WorkflowBindings
                .Where(b => b.IsActive
                    && b.ProcessCode == request.ProcessCode
                    && b.EffectiveFrom <= now
                    && (b.EffectiveTo == null || b.EffectiveTo > now))
                .Where(b => (b.ScopeType == "COMPANY" && b.CompanyId == request.CompanyId)
                    || b.ScopeType == "GLOBAL")
                .ToListAsync(ct);

            if (candidateBindings.Count == 0)
                throw new BusinessRuleValidationException("WF_NO_VALID_BINDING", "No active workflow binding found for this process and scope.");

            // Thứ tự ưu tiên: liên kết theo CÔNG TY thắng GLOBAL, sau đó mức ưu tiên cao hơn thắng.
            var rankedBindings = candidateBindings
                .OrderByDescending(b => b.ScopeType == "COMPANY" ? 1 : 0)
                .ThenByDescending(b => b.Priority)
                .ToList();

            // ĐÁNH GIÁ ĐIỀU KIỆN: giữ lại các liên kết mà điều kiện của phiên bản KHỚP payload.
            // Nhờ đó admin khai báo được luật kiểu "tổng tiền > 50 triệu thì dùng quy trình 2 cấp"
            // mà không cần lập trình viên. Không có điều kiện = luôn khớp (tương thích ngược).
            var candidateVersionIds = rankedBindings.Select(b => b.WorkflowVersionId).Distinct().ToList();
            var conditionsByVersion = (await context.WorkflowConditions
                    .AsNoTracking()
                    .Where(c => candidateVersionIds.Contains(c.WorkflowVersionId))
                    .ToListAsync(ct))
                .GroupBy(c => c.WorkflowVersionId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyCollection<ConditionCheck>)g
                        .Select(c => new ConditionCheck(c.FieldCode, c.Operator, c.Value)).ToList());

            var matchingBindings = rankedBindings
                .Where(b => WorkflowConditionEvaluator.Matches(
                    conditionsByVersion.TryGetValue(b.WorkflowVersionId, out var cs) ? cs : [],
                    request.PayloadJson))
                .ToList();

            if (matchingBindings.Count == 0)
                throw new BusinessRuleValidationException(
                    "WF_NO_MATCHING_CONDITION",
                    $"Quy trình '{request.ProcessCode}' có liên kết nhưng KHÔNG liên kết nào thoả điều kiện áp dụng " +
                    "cho hồ sơ này. Vui lòng kiểm tra điều kiện của các phiên bản, hoặc bổ sung một liên kết " +
                    "không điều kiện làm phương án mặc định.");

            var binding = matchingBindings[0];

            // Nếu còn liên kết khác NGANG HẠNG với cái thắng thì đây là lỗi cấu hình.
            // Trước đây hệ chọn bừa một cái — trái nguyên tắc "mập mờ là lỗi cấu hình,
            // không bao giờ chọn ngẫu nhiên" trong tài liệu quản trị.
            var tiedBindings = matchingBindings
                .Where(b => (b.ScopeType == "COMPANY") == (binding.ScopeType == "COMPANY")
                            && b.Priority == binding.Priority)
                .ToList();

            if (tiedBindings.Count > 1)
                throw new BusinessRuleValidationException(
                    "WF_BINDING_AMBIGUOUS",
                    $"Quy trình '{request.ProcessCode}' có {tiedBindings.Count} liên kết cùng phạm vi và cùng mức ưu tiên " +
                    $"({string.Join(", ", tiedBindings.Select(b => $"#{b.Id}"))}). Đây là lỗi cấu hình: " +
                    "vui lòng đóng bớt liên kết cũ hoặc đặt mức ưu tiên khác nhau.");

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

    public async Task<WorkflowInstanceDto?> GetInstanceByIdAsync(long instanceId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var instance = await context.WorkflowInstances.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct);
        if (instance == null)
            return null;

        // Kiểm quyền theo giao dịch: người đề xuất HOẶC người được giao duyệt HOẶC có WORKFLOW_VIEW.
        if (instance.RequesterId != actorUserId)
        {
            var isAssignee = await context.WorkflowInstanceStepAssignees
                .AsNoTracking()
                .AnyAsync(a => a.UserId == actorUserId && a.Step.WorkflowInstanceId == instanceId, ct);
            if (!isAssignee)
            {
                var canView = await _permissionEvaluator.EvaluateAsync(actorUserId, "WORKFLOW_VIEW", instance.CompanyId, ct)
                    || await _permissionEvaluator.EvaluateAsync(actorUserId, "WORKFLOW_VIEW", null, ct);
                if (!canView)
                    throw new BusinessRuleValidationException("WF_INSTANCE_VIEW_UNAUTHORIZED", "You do not have permission to view this workflow instance.");
            }
        }

        return await LoadInstanceDtoAsync(context, instanceId, ct);
    }

    public async Task<MyApprovalItemDto[]> GetMyPendingApprovalsAsync(long userId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var items = await context.WorkflowInstanceStepAssignees
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
                    AssignedAt = s.AssignedAt,
                    RequesterId = i.RequesterId
                })
            .OrderByDescending(a => a.AssignedAt)
            .ToArrayAsync(ct);

        // D1 — hiện tên người đề xuất để người duyệt biết ai gửi.
        var names = await ResolveUserNamesAsync(context, items.Select(i => i.RequesterId), ct);
        foreach (var i in items)
            if (names.TryGetValue(i.RequesterId, out var rn)) i.RequesterName = rn;

        // Nhãn đối tượng (tên gói + khách) để người duyệt biết đang duyệt cái gì.
        var pkgLabels = await ResolveCarePackageLabelsAsync(
            context, items.Where(i => i.BusinessEntityType == "CustomerCarePackage").Select(i => i.BusinessEntityId), ct);
        foreach (var i in items)
            if (i.BusinessEntityType == "CustomerCarePackage" && pkgLabels.TryGetValue(i.BusinessEntityId, out var lbl))
                i.BusinessEntityLabel = lbl;

        return items;
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
                if (handler == null)
                {
                    // Không có bộ xử lý: TRƯỚC ĐÂY im lặng bỏ qua, hồ sơ kẹt PENDING_EXECUTION
                    // vĩnh viễn và không chạy lại được. Nay đánh dấu FAILED để nổi lên hàng đợi lỗi
                    // và có thể chạy lại sau khi CNTT bổ sung bộ xử lý.
                    await MarkFailedAsync(instanceId, ct);
                    throw new BusinessRuleValidationException(
                        "WF_NO_EXECUTION_HANDLER",
                        $"Đã duyệt xong nhưng quy trình '{instance.ProcessCode}' chưa có bộ xử lý thực thi. " +
                        "Hồ sơ được đánh dấu Thất bại để xử lý lại. Vui lòng báo bộ phận CNTT.");
                }

                {
                    try
                    {
                        await handler.ExecuteAsync(instance, ct);
                    }
                    catch (Exception)
                    {
                        await MarkFailedAsync(instanceId, ct);
                        throw;
                    }

                    // Handler chạy xong (không ném) → lật hồ sơ sang EXECUTED (trước đây kẹt PENDING_EXECUTION).
                    await MarkExecutedAsync(instanceId, ct);
                }
            }

            return await LoadInstanceDtoAsync(context, instanceId, ct);
        });
    }

    // Đặt hồ sơ = FAILED khi bộ xử lý ném lỗi hoặc không tồn tại.
    private async Task MarkFailedAsync(long instanceId, CancellationToken ct)
    {
        await using var tempCtx = _dbContextFactory.CreateDbContext();
        var strategy = tempCtx.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var ctx = _dbContextFactory.CreateDbContext();
            var wi = await ctx.WorkflowInstances.FirstAsync(w => w.Id == instanceId, ct);
            if (wi.InstanceStatus == "PENDING_EXECUTION" || wi.InstanceStatus == "EXECUTING")
            {
                wi.SetFailed();
                await ctx.SaveChangesAsync(ct);
            }
        });
    }

    // Đặt hồ sơ = EXECUTED sau khi bộ xử lý chạy thành công.
    private async Task MarkExecutedAsync(long instanceId, CancellationToken ct)
    {
        await using var tempCtx = _dbContextFactory.CreateDbContext();
        var strategy = tempCtx.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var ctx = _dbContextFactory.CreateDbContext();
            var wi = await ctx.WorkflowInstances.FirstAsync(w => w.Id == instanceId, ct);
            if (wi.InstanceStatus == "PENDING_EXECUTION" || wi.InstanceStatus == "EXECUTING")
            {
                wi.SetExecuted(null);
                await ctx.SaveChangesAsync(ct);
            }
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

        var dtos = instances.Select(instance => new WorkflowInstanceDto
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

        await EnrichInstanceNamesAsync(context, dtos, ct);
        return dtos;
    }

    /// <summary>
    /// Tra cứu hồ sơ cho màn hình quản trị. Quyền đã được chặn ở controller (WORKFLOW_VIEW),
    /// nên ở đây không lọc theo người dùng — đây là góc nhìn toàn hệ thống.
    /// </summary>
    public async Task<PagedResult<WorkflowInstanceDto>> SearchInstancesAsync(WorkflowInstanceSearchRequest request, CancellationToken ct = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        await using var context = _dbContextFactory.CreateDbContext();

        var query = context.WorkflowInstances.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ProcessCode))
            query = query.Where(i => i.ProcessCode == request.ProcessCode);
        if (!string.IsNullOrWhiteSpace(request.InstanceStatus))
            query = query.Where(i => i.InstanceStatus == request.InstanceStatus);
        if (request.CompanyId.HasValue)
            query = query.Where(i => i.CompanyId == request.CompanyId.Value);
        if (request.RequesterId.HasValue)
            query = query.Where(i => i.RequesterId == request.RequesterId.Value);

        var totalCount = await query.LongCountAsync(ct);

        var instances = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);

        var dtos = instances.Select(instance => new WorkflowInstanceDto
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
            Steps = []
        }).ToArray();

        await EnrichInstanceNamesAsync(context, dtos, ct);

        return new PagedResult<WorkflowInstanceDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = dtos
        };
    }

    public async Task<bool?> IsApprovalRequiredAsync(string processCode, long? companyId, string? payloadJson, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var now = DateTime.UtcNow;

        var candidates = await context.WorkflowBindings
            .AsNoTracking()
            .Where(b => b.IsActive
                && b.ProcessCode == processCode
                && b.EffectiveFrom <= now
                && (b.EffectiveTo == null || b.EffectiveTo > now))
            .Where(b => (b.ScopeType == "COMPANY" && b.CompanyId == companyId) || b.ScopeType == "GLOBAL")
            .ToListAsync(ct);

        // Chưa cấu hình gì → engine không có cơ sở kết luận. KHÔNG được trả false,
        // vì như thế là âm thầm cho hồ sơ thoát phê duyệt.
        if (candidates.Count == 0) return null;

        var versionIds = candidates.Select(b => b.WorkflowVersionId).Distinct().ToList();
        var conditionsByVersion = (await context.WorkflowConditions
                .AsNoTracking()
                .Where(c => versionIds.Contains(c.WorkflowVersionId))
                .ToListAsync(ct))
            .GroupBy(c => c.WorkflowVersionId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<ConditionCheck>)g
                    .Select(c => new ConditionCheck(c.FieldCode, c.Operator, c.Value)).ToList());

        // Cần duyệt khi có ít nhất một liên kết mà điều kiện của phiên bản KHỚP dữ liệu hồ sơ.
        return candidates.Any(b => WorkflowConditionEvaluator.Matches(
            conditionsByVersion.TryGetValue(b.WorkflowVersionId, out var cs) ? cs : [],
            payloadJson));
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

        var actions = await context.WorkflowActions
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

        var names = await ResolveUserNamesAsync(
            context,
            actions.Select(a => a.ActedBy).Concat(actions.Where(a => a.OnBehalfOf.HasValue).Select(a => a.OnBehalfOf!.Value)),
            ct);
        foreach (var a in actions)
        {
            if (names.TryGetValue(a.ActedBy, out var an)) a.ActedByName = an;
            if (a.OnBehalfOf.HasValue && names.TryGetValue(a.OnBehalfOf.Value, out var on)) a.OnBehalfOfName = on;
        }
        return actions;
    }

    public async Task<WorkflowInstanceDto> RejectStepAsync(long instanceId, long stepId, ApprovalActionRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BusinessRuleValidationException("WF_REASON_REQUIRED", "Reason is required for reject action.");

        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        // Ghi nhận việc từ chối. Khối này có thể được CHẠY LẠI khi CSDL lỗi tạm thời,
        // nên KHÔNG được đặt việc hoàn tác nghiệp vụ ở trong (xem ngay sau khối).
        var dto = await strategy.ExecuteAsync(async () =>
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

        // Hoàn tác nghiệp vụ: đưa bản ghi ra khỏi trạng thái "chờ duyệt".
        // Không có bước này thì bản ghi (vd gói dịch vụ) kẹt "chờ duyệt" vĩnh viễn.
        // Đặt NGOÀI khối trên để lỗi ở đây không khiến việc từ chối (đã ghi nhận xong) bị chạy lại.
        var rejectHandler = _executionHandlerFactory.GetHandler(dto.ProcessCode);
        if (rejectHandler != null)
        {
            await using var compCtx = _dbContextFactory.CreateDbContext();
            var rejectedInstance = await compCtx.WorkflowInstances
                .AsNoTracking()
                .FirstAsync(i => i.Id == instanceId, ct);

            try
            {
                await rejectHandler.OnRejectedAsync(rejectedInstance, ct);
            }
            catch (Exception ex)
            {
                // Việc từ chối ĐÃ được ghi nhận; chỉ phần cập nhật bản ghi nghiệp vụ thất bại.
                // Báo rõ để không ai tưởng thao tác chưa chạy, và để bộ phận CNTT xử lý lại.
                throw new BusinessRuleValidationException(
                    "WF_REJECT_COMPENSATION_FAILED",
                    "Đã ghi nhận từ chối, nhưng chưa cập nhật được trạng thái hồ sơ nghiệp vụ. " +
                    $"Vui lòng báo bộ phận CNTT (hồ sơ #{instanceId}). Chi tiết: {ex.Message}");
            }
        }

        return dto;
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

            // Cho phép chạy lại cả hồ sơ đang KẸT ở PENDING_EXECUTION/EXECUTING — đây là những hồ sơ
            // mồ côi do bản cũ không có bộ xử lý (hoặc tiến trình chết giữa chừng); trước đây
            // chúng không cứu được bằng bất kỳ cách nào ngoài sửa CSDL tay.
            if (instance.InstanceStatus is not ("FAILED" or "PENDING_EXECUTION" or "EXECUTING"))
                throw new BusinessRuleValidationException(
                    "WF_INSTANCE_NOT_RETRYABLE",
                    "Chỉ chạy lại được hồ sơ đang Thất bại hoặc đang kẹt chờ thực thi.");

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
            if (handler == null)
            {
                await MarkFailedAsync(instanceId, ct);
                throw new BusinessRuleValidationException(
                    "WF_NO_EXECUTION_HANDLER",
                    $"Quy trình '{instance.ProcessCode}' vẫn chưa có bộ xử lý thực thi nên chưa chạy lại được. Vui lòng báo bộ phận CNTT.");
            }

            {
                try
                {
                    await handler.ExecuteAsync(instance, ct);
                }
                catch (Exception)
                {
                    await MarkFailedAsync(instanceId, ct);
                    throw; // Allow caller to see failure
                }

                // Chạy lại thành công → lật hồ sơ sang EXECUTED.
                await MarkExecutedAsync(instanceId, ct);
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

            var requesterWasCandidate = false;
            var hadOtherCandidates = false;
            foreach (var rule in templateStep.ApproverRules)
            {
                var resolution = await _approverResolver.ResolveApproversDetailedAsync(
                    rule.ApproverSourceType, rule.ApproverSourceValue, requesterId, companyId, instance.ProcessCode, ct);

                requesterWasCandidate |= resolution.RequesterWasCandidate;
                hadOtherCandidates |= resolution.HadOtherCandidates;

                foreach (var userId in resolution.Approvers)
                {
                    var assignee = new WorkflowInstanceStepAssignee(instanceStep.Id, userId, rule.ApproverSourceType);
                    context.WorkflowInstanceStepAssignees.Add(assignee);
                }
            }
            await context.SaveChangesAsync(ct);

            var assigneeCount = await context.WorkflowInstanceStepAssignees
                .CountAsync(a => a.WorkflowInstanceStepId == instanceStep.Id, ct);
            if (assigneeCount == 0)
            {
                // Tách 2 ca vốn bị gộp làm một:
                //  - Người đề xuất CHÍNH LÀ người duyệt duy nhất (vd trưởng phòng tự tạo) → hợp lệ,
                //    module nghiệp vụ tự quyết định có tự duyệt hay không.
                //  - Không tìm được ai duyệt → LỖI CẤU HÌNH, phải chặn.
                //
                // Ba điều kiện đều bắt buộc:
                //  (a) người đề xuất nằm trong nhóm người duyệt;
                //  (b) KHÔNG có ai khác được cấu hình (nếu có mà họ đã nghỉ việc thì là lỗi cấu hình);
                //  (c) quy trình chỉ có ĐÚNG MỘT bước — nhiều bước mà cho qua thì các cấp duyệt
                //      phía sau (vd Giám đốc) sẽ bị bỏ qua hoàn toàn.
                if (requesterWasCandidate && !hadOtherCandidates && sortedSteps.Count == 1)
                    throw new BusinessRuleValidationException(
                        "WF_ONLY_REQUESTER_IS_APPROVER",
                        $"Bước '{templateStep.StepName}': người đề xuất cũng chính là người duyệt duy nhất.");

                if (requesterWasCandidate && !hadOtherCandidates)
                    throw new BusinessRuleValidationException(
                        "WF_NO_ASSIGNEE_FOR_STEP",
                        $"Bước '{templateStep.StepName}': người đề xuất là người duyệt duy nhất, nhưng quy trình còn bước phê duyệt khác nên không thể tự duyệt. Vui lòng bổ sung người duyệt thay thế.");

                throw new BusinessRuleValidationException(
                    "WF_NO_ASSIGNEE_FOR_STEP",
                    $"Bước '{templateStep.StepName}' chưa xác định được người duyệt nào (người duyệt được cấu hình có thể đã nghỉ việc hoặc bị khoá tài khoản). Vui lòng kiểm tra cấu hình thẩm quyền phê duyệt.");
            }
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

        var dto = new WorkflowInstanceDto
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

        await EnrichInstanceNamesAsync(context, new[] { dto }, ct);

        if (dto.BusinessEntityType == "CustomerCarePackage")
        {
            var pkgLabels = await ResolveCarePackageLabelsAsync(context, new[] { dto.BusinessEntityId }, ct);
            if (pkgLabels.TryGetValue(dto.BusinessEntityId, out var lbl)) dto.BusinessEntityLabel = lbl;
        }

        return dto;
    }

    /// <summary>
    /// D1/D2 — bơm tên người (đề xuất / duyệt / được giao) vào DTO để giao diện hiện tên
    /// thay vì "Người dùng 123". Dữ liệu đã có sẵn (RequesterId, CompletedBy, assignee UserId);
    /// đây chỉ là một truy vấn tra tên duy nhất cho cả tập.
    /// </summary>
    private static async Task EnrichInstanceNamesAsync(IOrganizationDbContext context, IReadOnlyCollection<WorkflowInstanceDto> instances, CancellationToken ct)
    {
        if (instances.Count == 0) return;

        var ids = instances.Select(i => i.RequesterId)
            .Concat(instances.SelectMany(i => i.Steps).Where(s => s.CompletedBy.HasValue).Select(s => s.CompletedBy!.Value))
            .Concat(instances.SelectMany(i => i.Steps).SelectMany(s => s.Assignees).Select(a => a.UserId));

        var names = await ResolveUserNamesAsync(context, ids, ct);

        foreach (var i in instances)
        {
            if (names.TryGetValue(i.RequesterId, out var rn)) i.RequesterName = rn;
            foreach (var s in i.Steps)
            {
                if (s.CompletedBy.HasValue && names.TryGetValue(s.CompletedBy.Value, out var cn)) s.CompletedByName = cn;
                foreach (var a in s.Assignees)
                    if (names.TryGetValue(a.UserId, out var an)) a.UserName = an;
            }
        }
    }

    private static async Task<Dictionary<long, string>> ResolveUserNamesAsync(IOrganizationDbContext context, IEnumerable<long> userIds, CancellationToken ct)
    {
        var ids = userIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<long, string>();
        return await context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
    }

    /// <summary>
    /// Nhãn dễ hiểu cho đối tượng nghiệp vụ để người duyệt biết đang duyệt cái gì (thay vì "ID 7").
    /// Hiện xử lý CustomerCarePackage: "Tên gói — Tên khách (mã KH)". Loại khác trả rỗng → FE hiển thị mặc định.
    /// </summary>
    private static async Task<Dictionary<long, string>> ResolveCarePackageLabelsAsync(IOrganizationDbContext context, IEnumerable<long> packageIds, CancellationToken ct)
    {
        var ids = packageIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<long, string>();
        var rows = await (
            from p in context.CustomerCarePackages.AsNoTracking()
            where ids.Contains(p.Id)
            join st in context.ServiceTypes on p.ServiceTypeId equals st.Id
            join c in context.Customers on p.CustomerId equals c.Id
            join pr in context.Profiles on c.ProfileId equals pr.Id
            select new { p.Id, ServiceName = st.Name, CustomerName = pr.FullName, c.CustomerCode })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.Id, r => $"{r.ServiceName} — {r.CustomerName} ({r.CustomerCode})");
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
