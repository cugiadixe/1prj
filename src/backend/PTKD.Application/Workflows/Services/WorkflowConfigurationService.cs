using System;
using System.Linq;
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

namespace PTKD.Application.Workflows.Services;

public class WorkflowConfigurationService : IWorkflowConfigurationService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public WorkflowConfigurationService(IOrganizationDbContextFactory dbContextFactory, ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task<BusinessProcessDto[]> GetActiveBusinessProcessesAsync(CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        return await context.BusinessProcessCatalogs
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.ProcessCode)
            .Select(p => new BusinessProcessDto
            {
                ProcessCode = p.ProcessCode,
                ProcessName = p.ProcessName,
                Description = p.Description,
                IsApprovalRequired = p.IsApprovalRequired,
                IsActive = p.IsActive
            })
            .ToArrayAsync(ct);
    }

    public async Task<PagedResult<WorkflowDefinitionListItemDto>> SearchDefinitionsAsync(WorkflowSearchRequest request, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var query = context.WorkflowDefinitions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ProcessCode))
            query = query.Where(d => d.ProcessCode == request.ProcessCode);
        if (request.IsActive.HasValue)
            query = query.Where(d => d.IsActive == request.IsActive.Value);

        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new WorkflowDefinitionListItemDto
            {
                Id = d.Id,
                DefinitionCode = d.DefinitionCode,
                DefinitionName = d.DefinitionName,
                ProcessCode = d.ProcessCode,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt
            })
            .ToArrayAsync(ct);

        return new PagedResult<WorkflowDefinitionListItemDto> { Items = items, TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<WorkflowDefinitionDetailDto> CreateDefinitionAsync(CreateWorkflowDefinitionRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (!await context.BusinessProcessCatalogs.AnyAsync(p => p.ProcessCode == request.ProcessCode && p.IsActive, ct))
                throw new BusinessRuleValidationException("WF_INVALID_PROCESS_CODE", "Business process not found or inactive.");

            if (await context.WorkflowDefinitions.AnyAsync(d => d.DefinitionCode == request.DefinitionCode, ct))
                throw new BusinessRuleValidationException("WF_DUPLICATE_DEFINITION_CODE", "Definition code already exists.");

            var definition = new WorkflowDefinition(request.DefinitionCode, request.DefinitionName, request.ProcessCode, actorUserId, request.Description);
            context.WorkflowDefinitions.Add(definition);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_DEFINITION_CREATED",
                EntityType = "WorkflowDefinition",
                EntityId = definition.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { definition.DefinitionCode, definition.DefinitionName, definition.ProcessCode })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToDefinitionDetailDto(definition);
        });
    }

    public async Task<WorkflowDefinitionDetailDto?> GetDefinitionByIdAsync(long id, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var definition = await context.WorkflowDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        return definition == null ? null : MapToDefinitionDetailDto(definition);
    }

    public async Task<WorkflowDefinitionDetailDto> UpdateDefinitionAsync(long id, UpdateWorkflowDefinitionRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var definition = await context.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Id == id, ct)
                ?? throw new EntityNotFoundException("WF_DEFINITION_NOT_FOUND", "Workflow definition not found.");

            if (!definition.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The definition has been modified by another user.");

            var beforeName = definition.DefinitionName;
            definition.Update(request.DefinitionName, request.Description, actorUserId);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_DEFINITION_UPDATED",
                EntityType = "WorkflowDefinition",
                EntityId = definition.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                BeforeStateJson = JsonSerializer.Serialize(new { DefinitionName = beforeName }),
                AfterStateJson = JsonSerializer.Serialize(new { definition.DefinitionName })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToDefinitionDetailDto(definition);
        });
    }

    public async Task<WorkflowVersionListItemDto[]> GetVersionsByDefinitionIdAsync(long definitionId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        return await context.WorkflowDefinitionVersions
            .AsNoTracking()
            .Where(v => v.WorkflowDefinitionId == definitionId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new WorkflowVersionListItemDto
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                VersionStatus = v.VersionStatus,
                EffectiveFrom = v.EffectiveFrom,
                EffectiveTo = v.EffectiveTo,
                CreatedAt = v.CreatedAt
            })
            .ToArrayAsync(ct);
    }

    public async Task<WorkflowVersionDetailDto> CreateVersionAsync(long definitionId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var definition = await context.WorkflowDefinitions.FirstOrDefaultAsync(d => d.Id == definitionId, ct)
                ?? throw new EntityNotFoundException("WF_DEFINITION_NOT_FOUND", "Workflow definition not found.");

            var maxVersion = await context.WorkflowDefinitionVersions
                .Where(v => v.WorkflowDefinitionId == definitionId)
                .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0;

            var version = new WorkflowDefinitionVersion(definitionId, maxVersion + 1, actorUserId);
            context.WorkflowDefinitionVersions.Add(version);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_VERSION_CREATED",
                EntityType = "WorkflowDefinitionVersion",
                EntityId = version.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { version.WorkflowDefinitionId, version.VersionNumber })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToVersionDetailDto(version);
        });
    }

    public async Task<WorkflowVersionDetailDto?> GetVersionByIdAsync(long versionId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var version = await context.WorkflowDefinitionVersions
            .AsNoTracking()
            .Include(v => v.Steps).ThenInclude(s => s.ApproverRules)
            .Include(v => v.Conditions)
            .FirstOrDefaultAsync(v => v.Id == versionId, ct);

        return version == null ? null : MapToVersionDetailDto(version);
    }

    public async Task DeleteVersionAsync(long versionId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var version = await context.WorkflowDefinitionVersions
                .Include(v => v.Steps).ThenInclude(s => s.ApproverRules)
                .Include(v => v.Conditions)
                .FirstOrDefaultAsync(v => v.Id == versionId, ct)
                ?? throw new EntityNotFoundException("WF_VERSION_NOT_FOUND", "Workflow version not found.");

            if (!version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Only DRAFT versions can be deleted.");

            if (await context.WorkflowBindings.AnyAsync(b => b.WorkflowVersionId == versionId, ct))
                throw new BusinessRuleValidationException("WF_VERSION_HAS_BINDINGS", "Cannot delete version with bindings.");

            if (await context.WorkflowInstances.AnyAsync(i => i.WorkflowVersionId == versionId, ct))
                throw new BusinessRuleValidationException("WF_VERSION_HAS_INSTANCES", "Cannot delete version with instances.");

            context.WorkflowConditions.RemoveRange(version.Conditions);
            foreach (var step in version.Steps)
                context.WorkflowStepApproverRules.RemoveRange(step.ApproverRules);
            context.WorkflowSteps.RemoveRange(version.Steps);
            context.WorkflowDefinitionVersions.Remove(version);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_VERSION_DELETED",
                EntityType = "WorkflowDefinitionVersion",
                EntityId = version.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);
        });
    }

    public async Task<WorkflowStepDto> CreateStepAsync(long versionId, CreateWorkflowStepRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var version = await context.WorkflowDefinitionVersions.FirstOrDefaultAsync(v => v.Id == versionId, ct)
                ?? throw new EntityNotFoundException("WF_VERSION_NOT_FOUND", "Workflow version not found.");

            if (!version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Steps can only be added to DRAFT versions.");

            if (await context.WorkflowSteps.AnyAsync(s => s.WorkflowVersionId == versionId && s.StepOrder == request.StepOrder, ct))
                throw new BusinessRuleValidationException("WF_DUPLICATE_STEP_ORDER", "Step order already exists in this version.");

            var step = new WorkflowStep(versionId, request.StepOrder, request.StepName, request.IsRequired, request.Description);
            context.WorkflowSteps.Add(step);
            await context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return MapToStepDto(step);
        });
    }

    public async Task<WorkflowStepDto> UpdateStepAsync(long stepId, UpdateWorkflowStepRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var step = await context.WorkflowSteps
                .Include(s => s.Version)
                .Include(s => s.ApproverRules)
                .FirstOrDefaultAsync(s => s.Id == stepId, ct)
                ?? throw new EntityNotFoundException("WF_STEP_NOT_FOUND", "Workflow step not found.");

            if (!step.Version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Steps can only be modified in DRAFT versions.");

            if (!step.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The step has been modified by another user.");
            step.Update(request.StepName, request.StepOrder, request.IsRequired, request.Description, request.DueDurationMinutes);
            await context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return MapToStepDto(step);
        });
    }

    public async Task DeleteStepAsync(long stepId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var step = await context.WorkflowSteps
                .Include(s => s.Version)
                .Include(s => s.ApproverRules)
                .FirstOrDefaultAsync(s => s.Id == stepId, ct)
                ?? throw new EntityNotFoundException("WF_STEP_NOT_FOUND", "Workflow step not found.");

            if (!step.Version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Steps can only be deleted in DRAFT versions.");

            context.WorkflowStepApproverRules.RemoveRange(step.ApproverRules);
            context.WorkflowSteps.Remove(step);
            await context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        });
    }

    public async Task<ApproverRuleDto> CreateApproverRuleAsync(long stepId, CreateApproverRuleRequest request, long actorUserId, CancellationToken ct = default)
    {
        // A5 — chặn bẫy: chỉ chấp nhận đúng các loại nguồn mà ApproverResolver thực sự xử lý.
        // Bỏ DEPARTMENT_MANAGER/REQUESTER_MANAGER (resolver chưa hiện thực → tạo được nhưng ra 0
        // người duyệt, hồ sơ kẹt). Thêm APPROVAL_AUTHORITY (tra bảng Thẩm quyền phê duyệt).
        var validSourceTypes = new[] { "SPECIFIC_USER", "ROLE", "DEPARTMENT", "PERMISSION", "ADMIN_GROUP", "APPROVAL_AUTHORITY" };
        if (!validSourceTypes.Contains(request.ApproverSourceType))
            throw new BusinessRuleValidationException("WF_INVALID_APPROVER_SOURCE_TYPE", $"Invalid approver source type: {request.ApproverSourceType}");

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var step = await context.WorkflowSteps
                .Include(s => s.Version)
                .FirstOrDefaultAsync(s => s.Id == stepId, ct)
                ?? throw new EntityNotFoundException("WF_STEP_NOT_FOUND", "Workflow step not found.");

            if (!step.Version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Approver rules can only be added to DRAFT versions.");

            var rule = new WorkflowStepApproverRule(stepId, request.ApproverSourceType, request.ApproverSourceValue, request.Priority);
            context.WorkflowStepApproverRules.Add(rule);
            await context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            return new ApproverRuleDto
            {
                Id = rule.Id,
                ApproverSourceType = rule.ApproverSourceType,
                ApproverSourceValue = rule.ApproverSourceValue,
                Priority = rule.Priority
            };
        });
    }

    public async Task<WorkflowVersionDetailDto> PublishVersionAsync(long versionId, PublishVersionRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var version = await context.WorkflowDefinitionVersions
                .Include(v => v.Steps).ThenInclude(s => s.ApproverRules)
                .Include(v => v.Conditions)
                .FirstOrDefaultAsync(v => v.Id == versionId, ct)
                ?? throw new EntityNotFoundException("WF_VERSION_NOT_FOUND", "Workflow version not found.");

            if (!version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Only DRAFT versions can be published.");

            if (!version.Steps.Any())
                throw new BusinessRuleValidationException("WF_VERSION_NO_STEPS", "Version must have at least one step to publish.");

            foreach (var step in version.Steps)
            {
                if (!step.ApproverRules.Any())
                    throw new BusinessRuleValidationException("WF_STEP_NO_APPROVER_RULES", $"Step '{step.StepName}' must have at least one approver rule.");
            }

            if (!version.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The version has been modified by another user.");
            version.Publish(actorUserId, request.EffectiveFrom, request.EffectiveTo);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_VERSION_PUBLISHED",
                EntityType = "WorkflowDefinitionVersion",
                EntityId = version.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { version.VersionNumber, version.EffectiveFrom, version.EffectiveTo })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToVersionDetailDto(version);
        });
    }

    public async Task<WorkflowVersionDetailDto> ActivateVersionAsync(long versionId, string targetVersion, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(targetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var version = await context.WorkflowDefinitionVersions
                .Include(v => v.Steps).ThenInclude(s => s.ApproverRules)
                .Include(v => v.Conditions)
                .FirstOrDefaultAsync(v => v.Id == versionId, ct)
                ?? throw new EntityNotFoundException("WF_VERSION_NOT_FOUND", "Workflow version not found.");

            if (!version.IsPublished)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_PUBLISHED", "Only PUBLISHED versions can be activated.");

            if (!version.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The version has been modified by another user.");
            version.Activate();
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_VERSION_ACTIVATED",
                EntityType = "WorkflowDefinitionVersion",
                EntityId = version.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToVersionDetailDto(version);
        });
    }

    public async Task<WorkflowVersionDetailDto> RetireVersionAsync(long versionId, string targetVersion, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(targetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var version = await context.WorkflowDefinitionVersions
                .Include(v => v.Steps).ThenInclude(s => s.ApproverRules)
                .Include(v => v.Conditions)
                .FirstOrDefaultAsync(v => v.Id == versionId, ct)
                ?? throw new EntityNotFoundException("WF_VERSION_NOT_FOUND", "Workflow version not found.");

            if (!version.IsActive)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_ACTIVE", "Only ACTIVE versions can be retired.");

            if (!version.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The version has been modified by another user.");
            version.Retire();
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_VERSION_RETIRED",
                EntityType = "WorkflowDefinitionVersion",
                EntityId = version.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToVersionDetailDto(version);
        });
    }

    public async Task<WorkflowBindingListItemDto[]> GetBindingsAsync(string? processCode, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var query = context.WorkflowBindings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(processCode))
            query = query.Where(b => b.ProcessCode == processCode);

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => MapToBindingDto(b))
            .ToArrayAsync(ct);
    }

    public async Task<WorkflowBindingListItemDto> CreateBindingAsync(CreateWorkflowBindingRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var version = await context.WorkflowDefinitionVersions.FirstOrDefaultAsync(v => v.Id == request.WorkflowVersionId, ct)
                ?? throw new EntityNotFoundException("WF_VERSION_NOT_FOUND", "Workflow version not found.");

            if (!version.IsActive && !version.IsPublished)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_ACTIVE", "Binding requires a PUBLISHED or ACTIVE version.");

            if (!await context.BusinessProcessCatalogs.AnyAsync(p => p.ProcessCode == request.ProcessCode && p.IsActive, ct))
                throw new BusinessRuleValidationException("WF_INVALID_PROCESS_CODE", "Business process not found or inactive.");

            if (request.ScopeType == "COMPANY" && request.CompanyId == null)
                throw new BusinessRuleValidationException("WF_COMPANY_REQUIRED", "Company is required for COMPANY scope.");

            if (request.ScopeType == "GLOBAL" && request.CompanyId != null)
                throw new BusinessRuleValidationException("WF_COMPANY_NOT_ALLOWED", "Company must be null for GLOBAL scope.");

            var overlapping = await context.WorkflowBindings
                .Where(b => b.IsActive
                    && b.ProcessCode == request.ProcessCode
                    && b.ScopeType == request.ScopeType
                    && b.CompanyId == request.CompanyId
                    && b.Priority == request.Priority
                    && b.EffectiveFrom < (request.EffectiveTo ?? DateTime.MaxValue)
                    && (b.EffectiveTo == null || b.EffectiveTo > request.EffectiveFrom))
                .AnyAsync(ct);

            if (overlapping)
                throw new BusinessRuleValidationException("WF_BINDING_OVERLAP", "An active binding with overlapping effective period already exists.");

            var binding = new WorkflowBinding(request.WorkflowVersionId, request.ProcessCode, request.ScopeType, request.EffectiveFrom, actorUserId, request.CompanyId, request.Priority, request.EffectiveTo);
            context.WorkflowBindings.Add(binding);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_BINDING_CREATED",
                EntityType = "WorkflowBinding",
                EntityId = binding.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { binding.ProcessCode, binding.ScopeType, binding.CompanyId, binding.EffectiveFrom, binding.EffectiveTo })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToBindingDto(binding);
        });
    }

    public async Task<WorkflowBindingListItemDto> UpdateBindingAsync(long bindingId, UpdateWorkflowBindingRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var binding = await context.WorkflowBindings.FirstOrDefaultAsync(b => b.Id == bindingId, ct)
                ?? throw new EntityNotFoundException("WF_BINDING_NOT_FOUND", "Workflow binding not found.");

            if (!binding.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The binding has been modified by another user.");
            binding.Update(request.EffectiveFrom, request.EffectiveTo, request.Priority);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_BINDING_UPDATED",
                EntityType = "WorkflowBinding",
                EntityId = binding.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToBindingDto(binding);
        });
    }

    private static WorkflowDefinitionDetailDto MapToDefinitionDetailDto(WorkflowDefinition definition)
    {
        return new WorkflowDefinitionDetailDto
        {
            Id = definition.Id,
            DefinitionCode = definition.DefinitionCode,
            DefinitionName = definition.DefinitionName,
            Description = definition.Description,
            ProcessCode = definition.ProcessCode,
            IsActive = definition.IsActive,
            RowVersion = Convert.ToBase64String(definition.RowVersion),
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt
        };
    }

    private static WorkflowVersionDetailDto MapToVersionDetailDto(WorkflowDefinitionVersion version)
    {
        return new WorkflowVersionDetailDto
        {
            Id = version.Id,
            WorkflowDefinitionId = version.WorkflowDefinitionId,
            VersionNumber = version.VersionNumber,
            VersionStatus = version.VersionStatus,
            EffectiveFrom = version.EffectiveFrom,
            EffectiveTo = version.EffectiveTo,
            PublishedAt = version.PublishedAt,
            RowVersion = Convert.ToBase64String(version.RowVersion),
            CreatedAt = version.CreatedAt,
            Steps = version.Steps.OrderBy(s => s.StepOrder).Select(MapToStepDto).ToArray(),
            Conditions = version.Conditions.Select(c => new WorkflowConditionDto
            {
                Id = c.Id,
                FieldCode = c.FieldCode,
                Operator = c.Operator,
                Value = c.Value
            }).ToArray()
        };
    }

    private static WorkflowStepDto MapToStepDto(WorkflowStep step)
    {
        return new WorkflowStepDto
        {
            Id = step.Id,
            StepOrder = step.StepOrder,
            StepName = step.StepName,
            Description = step.Description,
            IsRequired = step.IsRequired,
            DueDurationMinutes = step.DueDurationMinutes,
            RowVersion = Convert.ToBase64String(step.RowVersion),
            ApproverRules = step.ApproverRules.OrderBy(r => r.Priority).Select(r => new ApproverRuleDto
            {
                Id = r.Id,
                ApproverSourceType = r.ApproverSourceType,
                ApproverSourceValue = r.ApproverSourceValue,
                Priority = r.Priority
            }).ToArray()
        };
    }

    private static WorkflowBindingListItemDto MapToBindingDto(WorkflowBinding binding)
    {
        return new WorkflowBindingListItemDto
        {
            Id = binding.Id,
            WorkflowVersionId = binding.WorkflowVersionId,
            ProcessCode = binding.ProcessCode,
            ScopeType = binding.ScopeType,
            CompanyId = binding.CompanyId,
            Priority = binding.Priority,
            EffectiveFrom = binding.EffectiveFrom,
            EffectiveTo = binding.EffectiveTo,
            IsActive = binding.IsActive,
            RowVersion = Convert.ToBase64String(binding.RowVersion)
        };
    }
}
