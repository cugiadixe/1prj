using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.ApprovalAuthorities.DTOs;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Domain.Entities;

namespace PTKD.Application.ApprovalAuthorities.Services;

public class ApprovalAuthorityService : IApprovalAuthorityService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public ApprovalAuthorityService(IOrganizationDbContextFactory dbContextFactory, ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task<ApprovalAuthorityDto[]> ListAsync(long? companyId, long? departmentId, bool includeClosed, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var query = context.ApprovalAuthorities.AsNoTracking().AsQueryable();

        if (companyId.HasValue)
            query = query.Where(a => a.CompanyId == companyId.Value);
        if (departmentId.HasValue)
            query = query.Where(a => a.DepartmentId == departmentId.Value);
        if (!includeClosed)
            query = query.Where(a => a.Status == ApprovalAuthority.StatusActive);

        var items = await query
            .OrderBy(a => a.CompanyId).ThenBy(a => a.DepartmentId).ThenBy(a => a.AuthorityLevel).ThenByDescending(a => a.Id)
            .Select(MapExpression())
            .ToArrayAsync(ct);

        await EnrichAsync(context, items, ct);
        return items;
    }

    public async Task<ApprovalAuthorityDto> CreateAsync(CreateApprovalAuthorityRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (request.AuthorityLevel <= 0)
            throw new BusinessRuleValidationException("AA_INVALID_LEVEL", "Cấp thẩm quyền phải lớn hơn 0.");
        if (request.MaxAmount.HasValue && request.MinAmount.HasValue && request.MaxAmount.Value < request.MinAmount.Value)
            throw new BusinessRuleValidationException("AA_INVALID_AMOUNT_RANGE", "Ngưỡng tiền tối đa không được nhỏ hơn tối thiểu.");
        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value <= request.EffectiveFrom)
            throw new BusinessRuleValidationException("AA_INVALID_EFFECTIVE_RANGE", "Ngày kết thúc hiệu lực phải sau ngày bắt đầu.");

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (!await context.Companies.AnyAsync(c => c.Id == request.CompanyId, ct))
                throw new EntityNotFoundException("AA_COMPANY_NOT_FOUND", "Không tìm thấy công ty.");

            var department = await context.Departments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.DepartmentId, ct);
            if (department == null)
                throw new EntityNotFoundException("AA_DEPARTMENT_NOT_FOUND", "Không tìm thấy phòng ban.");
            if (department.CompanyId != request.CompanyId)
                throw new BusinessRuleValidationException("AA_DEPARTMENT_COMPANY_MISMATCH", "Phòng ban không thuộc công ty đã chọn.");

            var approver = await context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.ApproverUserId, ct);
            if (approver == null)
                throw new EntityNotFoundException("AA_APPROVER_NOT_FOUND", "Không tìm thấy người duyệt.");
            if (approver.AccountStatus != "ACTIVE")
                throw new BusinessRuleValidationException("AA_APPROVER_INACTIVE", "Người duyệt không ở trạng thái hoạt động.");

            if (request.DelegatedFromUserId.HasValue
                && !await context.Users.AnyAsync(u => u.Id == request.DelegatedFromUserId.Value, ct))
                throw new EntityNotFoundException("AA_DELEGATOR_NOT_FOUND", "Không tìm thấy người uỷ quyền gốc.");

            if (!string.IsNullOrWhiteSpace(request.ProcessCode)
                && !await context.BusinessProcessCatalogs.AnyAsync(p => p.ProcessCode == request.ProcessCode, ct))
                throw new EntityNotFoundException("AA_PROCESS_NOT_FOUND", "Không tìm thấy mã quy trình.");

            var entity = ApprovalAuthority.Create(
                request.CompanyId, request.DepartmentId,
                string.IsNullOrWhiteSpace(request.ProcessCode) ? null : request.ProcessCode,
                request.ApproverUserId, request.AuthorityLevel,
                request.MinAmount, request.MaxAmount,
                request.EffectiveFrom, request.EffectiveTo,
                request.DelegatedFromUserId, request.Notes, actorUserId);

            context.ApprovalAuthorities.Add(entity);
            await context.SaveChangesAsync(ct);

            await WriteAuditAsync(context, "APPROVAL_AUTHORITY_CREATED", entity.Id, actorUserId, new
            {
                entity.CompanyId, entity.DepartmentId, entity.ProcessCode, entity.ApproverUserId,
                entity.AuthorityLevel, entity.MinAmount, entity.MaxAmount,
                entity.EffectiveFrom, entity.EffectiveTo, entity.DelegatedFromUserId
            }, ct);

            await transaction.CommitAsync(ct);

            return (await GetByIdEnrichedAsync(entity.Id, ct))!;
        });
    }

    public async Task<ApprovalAuthorityDto> CloseAsync(long id, CloseApprovalAuthorityRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var entity = await context.ApprovalAuthorities.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (entity == null)
                throw new EntityNotFoundException("AA_NOT_FOUND", "Không tìm thấy dòng thẩm quyền.");

            entity.Close(request.EffectiveTo, actorUserId);
            await context.SaveChangesAsync(ct);

            await WriteAuditAsync(context, "APPROVAL_AUTHORITY_CLOSED", entity.Id, actorUserId,
                new { entity.Id, request.EffectiveTo }, ct);

            await transaction.CommitAsync(ct);

            return (await GetByIdEnrichedAsync(entity.Id, ct))!;
        });
    }

    public async Task<ApproverOptionDto[]> ListApproverOptionsAsync(long companyId, long? departmentId, string? search, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Người đang được phân vào công ty này (active) + tài khoản còn hoạt động.
        var query =
            from uca in context.UserCompanyAssignments.AsNoTracking()
            where uca.CompanyId == companyId && uca.AssignmentStatus == "ACTIVE"
            join u in context.Users.AsNoTracking() on uca.UserId equals u.Id
            where u.AccountStatus == "ACTIVE"
            select new { u.Id, u.FullName, u.EmployeeCode };

        // Tuỳ chọn thu hẹp theo phòng ban đã chọn (người duyệt thường thuộc chính phòng đó).
        if (departmentId.HasValue)
        {
            var inDept = context.UserDepartmentAssignments.AsNoTracking()
                .Where(uda => uda.DepartmentId == departmentId.Value && uda.AssignmentStatus == "ACTIVE")
                .Select(uda => uda.UserId);
            query = query.Where(x => inDept.Contains(x.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.FullName.Contains(s) || x.EmployeeCode.Contains(s));
        }

        return await query
            .Distinct()
            .OrderBy(x => x.FullName)
            .Take(50)
            .Select(x => new ApproverOptionDto { Id = x.Id, FullName = x.FullName, EmployeeCode = x.EmployeeCode })
            .ToArrayAsync(ct);
    }

    private async Task<ApprovalAuthorityDto?> GetByIdEnrichedAsync(long id, CancellationToken ct)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var dto = await context.ApprovalAuthorities
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapExpression())
            .FirstOrDefaultAsync(ct);
        if (dto == null) return null;
        var arr = new[] { dto };
        await EnrichAsync(context, arr, ct);
        return dto;
    }

    private async Task WriteAuditAsync(IOrganizationDbContext context, string eventCode, long entityId, long actorUserId, object afterState, CancellationToken ct)
    {
        var audit = new SecurityAuditEventRecord
        {
            EventCode = eventCode,
            EntityType = "ApprovalAuthority",
            EntityId = entityId.ToString(),
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            AfterStateJson = JsonSerializer.Serialize(afterState)
        };
        audit.ThrowIfContainsSensitiveData();
        await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);
    }

    private static System.Linq.Expressions.Expression<Func<ApprovalAuthority, ApprovalAuthorityDto>> MapExpression()
    {
        return a => new ApprovalAuthorityDto
        {
            Id = a.Id,
            CompanyId = a.CompanyId,
            DepartmentId = a.DepartmentId,
            ProcessCode = a.ProcessCode,
            ApproverUserId = a.ApproverUserId,
            AuthorityLevel = a.AuthorityLevel,
            MinAmount = a.MinAmount,
            MaxAmount = a.MaxAmount,
            EffectiveFrom = a.EffectiveFrom,
            EffectiveTo = a.EffectiveTo,
            DelegatedFromUserId = a.DelegatedFromUserId,
            Status = a.Status,
            Notes = a.Notes,
            RowVersion = Convert.ToBase64String(a.RowVersion),
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };
    }

    private static async Task EnrichAsync(IOrganizationDbContext context, IReadOnlyCollection<ApprovalAuthorityDto> dtos, CancellationToken ct)
    {
        if (dtos.Count == 0) return;

        var companyIds = dtos.Select(d => d.CompanyId).Distinct().ToArray();
        var companyInfo = await context.Companies.AsNoTracking()
            .Where(c => companyIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var departmentIds = dtos.Select(d => d.DepartmentId).Distinct().ToArray();
        var departmentInfo = await context.Departments.AsNoTracking()
            .Where(d => departmentIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name })
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var userIds = dtos.Select(d => d.ApproverUserId)
            .Concat(dtos.Where(d => d.DelegatedFromUserId.HasValue).Select(d => d.DelegatedFromUserId!.Value))
            .Distinct().ToArray();
        var userInfo = await context.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        foreach (var d in dtos)
        {
            if (companyInfo.TryGetValue(d.CompanyId, out var cn)) d.CompanyName = cn;
            if (departmentInfo.TryGetValue(d.DepartmentId, out var dn)) d.DepartmentName = dn;
            if (userInfo.TryGetValue(d.ApproverUserId, out var an)) d.ApproverName = an;
            if (d.DelegatedFromUserId.HasValue && userInfo.TryGetValue(d.DelegatedFromUserId.Value, out var dfn))
                d.DelegatedFromName = dfn;
        }
    }
}
