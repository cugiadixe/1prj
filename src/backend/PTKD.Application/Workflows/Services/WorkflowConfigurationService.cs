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
    private readonly PTKD.Application.Security.Authorization.Interfaces.IAuthorizationDbContext _authContext;

    /// <summary>
    /// A5 — chặn bẫy: chỉ chấp nhận đúng các loại nguồn mà ApproverResolver THỰC SỰ xử lý.
    /// Bỏ DEPARTMENT_MANAGER/REQUESTER_MANAGER (resolver chưa hiện thực → tạo được nhưng ra 0
    /// người duyệt, hồ sơ kẹt). Có APPROVAL_AUTHORITY (tra bảng Thẩm quyền phê duyệt).
    /// Dùng chung cho cả tạo mới và sửa để hai đường không lệch nhau.
    /// </summary>
    public static readonly string[] ValidApproverSourceTypes =
        ["SPECIFIC_USER", "ROLE", "DEPARTMENT", "PERMISSION", "ADMIN_GROUP", "APPROVAL_AUTHORITY"];

    public WorkflowConfigurationService(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter,
        PTKD.Application.Security.Authorization.Interfaces.IAuthorizationDbContext authContext)
    {
        _authContext = authContext;
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
        if (!ValidApproverSourceTypes.Contains(request.ApproverSourceType))
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

    public async Task<ApproverRuleDto> UpdateApproverRuleAsync(long ruleId, CreateApproverRuleRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (!ValidApproverSourceTypes.Contains(request.ApproverSourceType))
            throw new BusinessRuleValidationException("WF_INVALID_APPROVER_SOURCE_TYPE", $"Invalid approver source type: {request.ApproverSourceType}");

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var rule = await context.WorkflowStepApproverRules
                .Include(r => r.Step).ThenInclude(s => s.Version)
                .FirstOrDefaultAsync(r => r.Id == ruleId, ct)
                ?? throw new EntityNotFoundException("WF_APPROVER_RULE_NOT_FOUND", "Không tìm thấy luật người duyệt.");

            if (!rule.Step.Version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Chỉ sửa được luật người duyệt trên bản nháp.");

            rule.Update(request.ApproverSourceType, request.ApproverSourceValue, request.Priority);
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

    public async Task DeleteApproverRuleAsync(long ruleId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var rule = await context.WorkflowStepApproverRules
                .Include(r => r.Step).ThenInclude(s => s.Version)
                .FirstOrDefaultAsync(r => r.Id == ruleId, ct)
                ?? throw new EntityNotFoundException("WF_APPROVER_RULE_NOT_FOUND", "Không tìm thấy luật người duyệt.");

            if (!rule.Step.Version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Chỉ xoá được luật người duyệt trên bản nháp.");

            context.WorkflowStepApproverRules.Remove(rule);
            await context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        });
    }

    /// <summary>
    /// Nhân bản một phiên bản thành bản NHÁP mới, sao chép nguyên vẹn bước + luật người duyệt
    /// + điều kiện. Không có việc này thì muốn sửa một bước trong quy trình 5 bước phải gõ lại
    /// cả 5 bước — lý do thực tế khiến không ai dám sửa quy trình đang chạy.
    /// </summary>
    public async Task<WorkflowVersionDetailDto> CloneVersionAsync(long sourceVersionId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var source = await context.WorkflowDefinitionVersions
                .Include(v => v.Steps).ThenInclude(s => s.ApproverRules)
                .Include(v => v.Conditions)
                .FirstOrDefaultAsync(v => v.Id == sourceVersionId, ct)
                ?? throw new EntityNotFoundException("WF_VERSION_NOT_FOUND", "Không tìm thấy phiên bản nguồn.");

            var maxVersion = await context.WorkflowDefinitionVersions
                .Where(v => v.WorkflowDefinitionId == source.WorkflowDefinitionId)
                .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0;

            var clone = new WorkflowDefinitionVersion(source.WorkflowDefinitionId, maxVersion + 1, actorUserId);
            context.WorkflowDefinitionVersions.Add(clone);
            await context.SaveChangesAsync(ct); // cần Id của bản mới trước khi tạo bước

            foreach (var sourceStep in source.Steps.OrderBy(s => s.StepOrder))
            {
                var newStep = new WorkflowStep(
                    clone.Id, sourceStep.StepOrder, sourceStep.StepName,
                    sourceStep.IsRequired, sourceStep.Description);
                // Giữ nguyên hạn xử lý nếu bản gốc có đặt.
                newStep.Update(sourceStep.StepName, sourceStep.StepOrder, sourceStep.IsRequired,
                    sourceStep.Description, sourceStep.DueDurationMinutes);
                context.WorkflowSteps.Add(newStep);
                await context.SaveChangesAsync(ct); // cần Id của bước trước khi tạo luật

                foreach (var sourceRule in sourceStep.ApproverRules)
                {
                    context.WorkflowStepApproverRules.Add(new WorkflowStepApproverRule(
                        newStep.Id, sourceRule.ApproverSourceType, sourceRule.ApproverSourceValue, sourceRule.Priority));
                }
            }

            foreach (var sourceCondition in source.Conditions)
            {
                context.WorkflowConditions.Add(new WorkflowCondition(
                    clone.Id, sourceCondition.FieldCode, sourceCondition.Operator, sourceCondition.Value));
            }

            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_VERSION_CLONED",
                EntityType = "WorkflowDefinitionVersion",
                EntityId = clone.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new
                {
                    SourceVersionId = sourceVersionId,
                    SourceVersionNumber = source.VersionNumber,
                    NewVersionId = clone.Id,
                    NewVersionNumber = clone.VersionNumber,
                    StepCount = source.Steps.Count
                })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            var reloaded = await context.WorkflowDefinitionVersions
                .AsNoTracking()
                .Include(v => v.Steps).ThenInclude(s => s.ApproverRules)
                .Include(v => v.Conditions)
                .FirstAsync(v => v.Id == clone.Id, ct);

            return MapToVersionDetailDto(reloaded);
        });
    }

    /// <summary>
    /// Nguồn dữ liệu cho ô "giá trị nguồn" của luật người duyệt. Trước đây là ô nhập tự do, admin
    /// phải nhớ và gõ đúng số ID người dùng / phòng ban hoặc mã vai trò — gõ sai thì hồ sơ không
    /// tìm được người duyệt và kẹt. Endpoint này gắn quyền WORKFLOW_CONFIG_MANAGE nên admin quy
    /// trình dùng được mà không cần quyền quản trị người dùng.
    /// </summary>
    public async Task<ApproverSourceOptionDto[]> GetApproverSourceOptionsAsync(string sourceType, CancellationToken ct = default)
    {
        switch (sourceType)
        {
            case "ROLE":
                return await _authContext.Roles.AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Name)
                    .Select(r => new ApproverSourceOptionDto { Value = r.RoleCode, Label = r.Name, Hint = r.RoleCode })
                    .ToArrayAsync(ct);

            case "ADMIN_GROUP":
                return await _authContext.AdminGroups.AsNoTracking()
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.Name)
                    .Select(g => new ApproverSourceOptionDto { Value = g.GroupCode, Label = g.Name, Hint = g.GroupCode })
                    .ToArrayAsync(ct);

            case "PERMISSION":
                return await _authContext.Permissions.AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.PermissionCode)
                    .Select(p => new ApproverSourceOptionDto { Value = p.PermissionCode, Label = p.PermissionCode, Hint = p.ModuleCode })
                    .ToArrayAsync(ct);

            case "SPECIFIC_USER":
                return await _authContext.Users.AsNoTracking()
                    .Where(u => u.AccountStatus == "ACTIVE")
                    .OrderBy(u => u.FullName)
                    .Select(u => new ApproverSourceOptionDto
                    {
                        Value = u.Id.ToString(),
                        Label = u.FullName,
                        Hint = u.EmployeeCode
                    })
                    .ToArrayAsync(ct);

            case "DEPARTMENT":
            {
                await using var context = _dbContextFactory.CreateDbContext();
                return await (
                    from d in context.Departments.AsNoTracking().Where(d => d.IsActive)
                    join c in context.Companies.AsNoTracking() on d.CompanyId equals c.Id into cj
                    from c in cj.DefaultIfEmpty()
                    orderby d.Name
                    select new ApproverSourceOptionDto
                    {
                        Value = d.Id.ToString(),
                        Label = d.Name,
                        Hint = c != null ? c.Name : null
                    }).ToArrayAsync(ct);
            }

            case "APPROVAL_AUTHORITY":
                // Giá trị là CẤP thẩm quyền, khớp bảng Thẩm quyền phê duyệt.
                return
                [
                    new ApproverSourceOptionDto { Value = "1", Label = "Cấp 1 — Trưởng phòng", Hint = "Tra bảng Thẩm quyền phê duyệt" },
                    new ApproverSourceOptionDto { Value = "2", Label = "Cấp 2 — Giám đốc", Hint = "Tra bảng Thẩm quyền phê duyệt" },
                ];

            default:
                throw new BusinessRuleValidationException(
                    "WF_INVALID_APPROVER_SOURCE_TYPE", $"Loại nguồn người duyệt không hợp lệ: {sourceType}");
        }
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

    /// <summary>
    /// Các phiên bản có thể gán liên kết cho một mã quy trình. Trước đây admin phải tự nhớ và
    /// GÕ SỐ ID phiên bản vào ô nhập — gõ nhầm sang phiên bản chưa kích hoạt là chặn cả quy trình.
    /// Chỉ trả về phiên bản ACTIVE vì lúc tạo hồ sơ engine bắt buộc phiên bản phải ACTIVE.
    /// </summary>
    public async Task<ApproverSourceOptionDto[]> GetBindableVersionsAsync(string processCode, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        return await (
            from v in context.WorkflowDefinitionVersions.AsNoTracking()
            join d in context.WorkflowDefinitions.AsNoTracking() on v.WorkflowDefinitionId equals d.Id
            where d.ProcessCode == processCode && d.IsActive && v.VersionStatus == "ACTIVE"
            orderby d.DefinitionName, v.VersionNumber
            select new ApproverSourceOptionDto
            {
                Value = v.Id.ToString(),
                Label = $"{d.DefinitionName} — phiên bản {v.VersionNumber}",
                Hint = d.DefinitionCode
            }).ToArrayAsync(ct);
    }

    public async Task<WorkflowBindingListItemDto> DeactivateBindingAsync(long bindingId, string targetVersion, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(targetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var binding = await context.WorkflowBindings.FirstOrDefaultAsync(b => b.Id == bindingId, ct)
                ?? throw new EntityNotFoundException("WF_BINDING_NOT_FOUND", "Không tìm thấy liên kết quy trình.");

            if (!binding.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("WF_INVALID_ROW_VERSION", "The binding has been modified by another user.");

            binding.Deactivate();
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_BINDING_DEACTIVATED",
                EntityType = "WorkflowBinding",
                EntityId = binding.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { binding.ProcessCode, binding.ScopeType, binding.CompanyId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToBindingDto(binding);
        });
    }

    public async Task<ConditionFieldDto[]> GetConditionFieldsAsync(string processCode, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        var fields = await context.WorkflowConditionFields
            .AsNoTracking()
            .Where(f => f.ProcessCode == processCode && f.IsActive)
            .OrderBy(f => f.FieldLabel)
            .ToListAsync(ct);

        return fields.Select(f => new ConditionFieldDto
        {
            FieldCode = f.FieldCode,
            FieldLabel = f.FieldLabel,
            DataType = f.DataType,
            Description = f.Description,
            AllowedOperators = WorkflowConditionEvaluator.OperatorsForDataType(f.DataType)
        }).ToArray();
    }

    public async Task<WorkflowConditionDto> CreateConditionAsync(long versionId, CreateWorkflowConditionRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var version = await context.WorkflowDefinitionVersions
                .Include(v => v.Definition)
                .FirstOrDefaultAsync(v => v.Id == versionId, ct)
                ?? throw new EntityNotFoundException("WF_VERSION_NOT_FOUND", "Không tìm thấy phiên bản quy trình.");

            if (!version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Chỉ thêm được điều kiện trên bản nháp.");

            // Ranh giới quản trị: trường phải nằm trong danh mục DEV khai báo sẵn cho quy trình này.
            var field = await context.WorkflowConditionFields
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.ProcessCode == version.Definition.ProcessCode
                                          && f.FieldCode == request.FieldCode
                                          && f.IsActive, ct)
                ?? throw new BusinessRuleValidationException(
                    "WF_INVALID_CONDITION_FIELD",
                    $"Trường '{request.FieldCode}' không được phép dùng làm điều kiện cho quy trình này.");

            var allowedOperators = WorkflowConditionEvaluator.OperatorsForDataType(field.DataType);
            if (!allowedOperators.Contains(request.Operator))
                throw new BusinessRuleValidationException(
                    "WF_INVALID_CONDITION_OPERATOR",
                    $"Toán tử '{request.Operator}' không dùng được với trường kiểu {field.DataType}. " +
                    $"Toán tử hợp lệ: {string.Join(", ", allowedOperators)}.");

            if (string.IsNullOrWhiteSpace(request.Value))
                throw new BusinessRuleValidationException("WF_CONDITION_VALUE_REQUIRED", "Giá trị so sánh là bắt buộc.");

            var condition = new WorkflowCondition(versionId, request.FieldCode, request.Operator, request.Value.Trim());
            context.WorkflowConditions.Add(condition);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "WORKFLOW_CONDITION_CREATED",
                EntityType = "WorkflowCondition",
                EntityId = condition.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { versionId, condition.FieldCode, condition.Operator, condition.Value })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return new WorkflowConditionDto
            {
                Id = condition.Id,
                FieldCode = condition.FieldCode,
                Operator = condition.Operator,
                Value = condition.Value
            };
        });
    }

    public async Task DeleteConditionAsync(long conditionId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var condition = await context.WorkflowConditions
                .Include(c => c.Version)
                .FirstOrDefaultAsync(c => c.Id == conditionId, ct)
                ?? throw new EntityNotFoundException("WF_CONDITION_NOT_FOUND", "Không tìm thấy điều kiện.");

            if (!condition.Version.IsDraft)
                throw new BusinessRuleValidationException("WF_VERSION_NOT_DRAFT", "Chỉ xoá được điều kiện trên bản nháp.");

            context.WorkflowConditions.Remove(condition);
            await context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
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
