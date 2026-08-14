using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.CustomerCarePackages.DTOs;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.DTOs;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.CustomerCarePackages.Services;

public class CustomerCarePackageService : ICustomerCarePackageService
{
    private const string AssignProcessCode = "ASSIGN_CARE_PACKAGE";

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;
    private readonly IWorkflowRuntimeService _workflowRuntimeService;

    public CustomerCarePackageService(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter,
        IWorkflowRuntimeService workflowRuntimeService)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
        _workflowRuntimeService = workflowRuntimeService;
    }

    public async Task<CustomerCarePackageDto[]> ListByCustomerAsync(long customerId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var items = await context.CustomerCarePackages
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.Id)
            .Select(MapExpression())
            .ToArrayAsync(ct);
        await EnrichAsync(context, items, ct);
        return items;
    }

    public async Task<CustomerCarePackageDto[]> ListByGraveAsync(long graveId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var items = await context.CustomerCarePackages
            .AsNoTracking()
            .Where(p => p.GraveId == graveId)
            .OrderByDescending(p => p.Id)
            .Select(MapExpression())
            .ToArrayAsync(ct);
        await EnrichAsync(context, items, ct);
        return items;
    }

    public async Task<CustomerCarePackageDto> CreateAsync(long? companyId, CreateCustomerCarePackageRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (request.CotCount <= 0)
            throw new BusinessRuleValidationException("CCP_INVALID_COT_COUNT", "Số cốt phải lớn hơn 0.");

        // Có phê duyệt hay không: chỉ khi có công ty VÀ đã cấu hình quy trình (binding) đang hiệu lực
        // cho ASSIGN_CARE_PACKAGE. Chưa cấu hình → gán thẳng như hành vi cũ (tương thích ngược).
        bool requiresApproval = companyId is > 0 && await HasActiveAssignBindingAsync(companyId.Value, ct);

        long packageId;
        await using (var tempContext = _dbContextFactory.CreateDbContext())
        {
            var strategy = tempContext.CreateExecutionStrategy();
            packageId = await strategy.ExecuteAsync(async () =>
            {
                await using var context = _dbContextFactory.CreateDbContext();
                await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

                if (!await context.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
                    throw new EntityNotFoundException("CCP_CUSTOMER_NOT_FOUND", "Không tìm thấy khách hàng.");

                var serviceType = await context.ServiceTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.ServiceTypeId, ct);
                if (serviceType == null || !serviceType.IsActive)
                    throw new EntityNotFoundException("CCP_SERVICE_TYPE_NOT_FOUND", "Không tìm thấy gói chăm sóc (hoặc đã ngừng).");

                var unitPrice = serviceType.StandardPrice;
                DateTime? endDate = serviceType.CycleDurationMonths.HasValue
                    ? request.StartDate.AddMonths(serviceType.CycleDurationMonths.Value).AddDays(-1)
                    : (DateTime?)null;

                var entity = CustomerCarePackage.Create(
                    request.CustomerId, request.ServiceTypeId, request.CotCount,
                    unitPrice, request.StartDate, endDate, request.Notes, actorUserId, requiresApproval);
                context.CustomerCarePackages.Add(entity);
                await context.SaveChangesAsync(ct);

                var eventCode = requiresApproval ? "CARE_PACKAGE_SUBMIT_APPROVAL" : "CARE_PACKAGE_ASSIGN_CUSTOMER";
                await WriteAuditAsync(context, eventCode, entity.Id, actorUserId,
                    new { entity.CustomerId, entity.ServiceTypeId, entity.CotCount, entity.TotalPrice, requiresApproval }, ct);

                await transaction.CommitAsync(ct);
                return entity.Id;
            });
        }

        // Không phê duyệt → gói đã ở PENDING_GRAVE, xong.
        if (!requiresApproval)
            return (await GetByIdEnrichedAsync(packageId, ct))!;

        // Có phê duyệt → sinh hồ sơ quy trình. Mirror CarePackageRequestService.SubmitAsync:
        // không bọc trong transaction ngoài vì CreateInstanceAsync tự quản transaction riêng.
        var workflowRequest = new CreateWorkflowInstanceRequest
        {
            ProcessCode = AssignProcessCode,
            BusinessEntityType = "CustomerCarePackage",
            BusinessEntityId = packageId,
            CompanyId = companyId,
            PayloadJson = JsonSerializer.Serialize(new { request.CustomerId, request.ServiceTypeId, request.CotCount })
        };

        try
        {
            var instance = await _workflowRuntimeService.CreateInstanceAsync(workflowRequest, actorUserId, ct);
            await LinkWorkflowInstanceAsync(packageId, instance.Id, actorUserId, ct);
        }
        catch (BusinessRuleValidationException ex) when (
            ex.ErrorCode == "WF_NO_ASSIGNEE_FOR_STEP" || ex.ErrorCode == "WF_NO_VALID_BINDING")
        {
            // Không có ai duyệt ngoài chính người tạo (VD trưởng phòng tự tạo — Q6),
            // hoặc binding vừa bị gỡ: TỰ ĐỘNG DUYỆT có ghi dấu, chuyển thẳng sang chờ gán mộ.
            await AutoApproveAsync(packageId, actorUserId, ex.ErrorCode, ct);
        }

        return (await GetByIdEnrichedAsync(packageId, ct))!;
    }

    private async Task<bool> HasActiveAssignBindingAsync(long companyId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await using var context = _dbContextFactory.CreateDbContext();
        return await context.WorkflowBindings
            .AsNoTracking()
            .AnyAsync(b => b.IsActive
                && b.ProcessCode == AssignProcessCode
                && b.EffectiveFrom <= now
                && (b.EffectiveTo == null || b.EffectiveTo > now)
                && ((b.ScopeType == "COMPANY" && b.CompanyId == companyId) || b.ScopeType == "GLOBAL"), ct);
    }

    private async Task LinkWorkflowInstanceAsync(long packageId, long instanceId, long actorUserId, CancellationToken ct)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var package = await context.CustomerCarePackages.FirstAsync(p => p.Id == packageId, ct);
            package.SetWorkflowInstance(instanceId, actorUserId);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
    }

    private async Task AutoApproveAsync(long packageId, long actorUserId, string reasonCode, CancellationToken ct)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var package = await context.CustomerCarePackages.FirstAsync(p => p.Id == packageId, ct);
            if (package.Status == CustomerCarePackage.StatusPendingApproval)
            {
                package.MarkApproved(actorUserId);
                await context.SaveChangesAsync(ct);
                await WriteAuditAsync(context, "CARE_PACKAGE_AUTO_APPROVED", package.Id, actorUserId,
                    new { package.Id, reason = reasonCode == "WF_NO_ASSIGNEE_FOR_STEP"
                        ? "Người đề xuất là người duyệt duy nhất — tự động duyệt."
                        : "Chưa có quy trình phê duyệt hiệu lực — gán thẳng." }, ct);
            }
            await transaction.CommitAsync(ct);
        });
    }

    public async Task<CustomerCarePackageDto> AssignGraveAsync(long id, long graveId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var package = await context.CustomerCarePackages.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (package == null)
                throw new EntityNotFoundException("CCP_NOT_FOUND", "Không tìm thấy gói chăm sóc của khách.");
            if (package.Status == CustomerCarePackage.StatusCancelled)
                throw new BusinessRuleValidationException("CCP_CANCELLED", "Gói đã hủy, không thể gán mộ.");

            var grave = await context.Graves.AsNoTracking().FirstOrDefaultAsync(g => g.Id == graveId, ct);
            if (grave == null)
                throw new EntityNotFoundException("CCP_GRAVE_NOT_FOUND", "Không tìm thấy phần mộ.");

            // 1. Mộ phải thuộc sở hữu của khách
            if (grave.OwnerCustomerId != package.CustomerId)
                throw new BusinessRuleValidationException("CCP_GRAVE_NOT_OWNED",
                    "Phần mộ không thuộc sở hữu của khách hàng này.");

            // 2. Số cốt phải khớp chính xác
            if (grave.CotCount != package.CotCount)
                throw new BusinessRuleValidationException("CCP_COT_COUNT_MISMATCH",
                    $"Số cốt không khớp: gói dành cho {package.CotCount} cốt nhưng mộ có {grave.CotCount} cốt.");

            // 3. Không gán trùng gói cùng loại đang hiệu lực trên cùng một mộ
            var duplicate = await context.CustomerCarePackages.AnyAsync(p =>
                p.Id != package.Id &&
                p.GraveId == graveId &&
                p.ServiceTypeId == package.ServiceTypeId &&
                p.Status == CustomerCarePackage.StatusActive, ct);
            if (duplicate)
                throw new BusinessRuleValidationException("CCP_DUPLICATE_ON_GRAVE",
                    "Mộ này đã có một gói cùng loại đang hiệu lực.");

            package.AssignGrave(graveId, actorUserId);
            await context.SaveChangesAsync(ct);

            await WriteAuditAsync(context, "CARE_PACKAGE_ASSIGN_GRAVE", package.Id, actorUserId,
                new { package.Id, graveId, package.CotCount }, ct);

            await transaction.CommitAsync(ct);

            return (await GetByIdEnrichedAsync(package.Id, ct))!;
        });
    }

    public async Task<CustomerCarePackageDto> CancelAsync(long id, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var package = await context.CustomerCarePackages.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (package == null)
                throw new EntityNotFoundException("CCP_NOT_FOUND", "Không tìm thấy gói chăm sóc của khách.");

            package.Cancel(actorUserId);
            await context.SaveChangesAsync(ct);

            await WriteAuditAsync(context, "CARE_PACKAGE_CANCEL", package.Id, actorUserId, new { package.Id }, ct);

            await transaction.CommitAsync(ct);

            return (await GetByIdEnrichedAsync(package.Id, ct))!;
        });
    }

    private async Task<CustomerCarePackageDto?> GetByIdEnrichedAsync(long id, CancellationToken ct)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var dto = await context.CustomerCarePackages
            .AsNoTracking()
            .Where(p => p.Id == id)
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
            EntityType = "CustomerCarePackage",
            EntityId = entityId.ToString(),
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            AfterStateJson = JsonSerializer.Serialize(afterState)
        };
        audit.ThrowIfContainsSensitiveData();
        await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);
    }

    private static System.Linq.Expressions.Expression<Func<CustomerCarePackage, CustomerCarePackageDto>> MapExpression()
    {
        return p => new CustomerCarePackageDto
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            ServiceTypeId = p.ServiceTypeId,
            GraveId = p.GraveId,
            CotCount = p.CotCount,
            UnitPrice = p.UnitPrice,
            TotalPrice = p.TotalPrice,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            Notes = p.Notes,
            WorkflowInstanceId = p.WorkflowInstanceId,
            RowVersion = Convert.ToBase64String(p.RowVersion),
            CreatedAt = p.CreatedAt,
            CreatedByUserId = p.CreatedByUserId,
            UpdatedAt = p.UpdatedAt,
            UpdatedByUserId = p.UpdatedByUserId
        };
    }

    private static async Task EnrichAsync(IOrganizationDbContext context, IReadOnlyCollection<CustomerCarePackageDto> dtos, CancellationToken ct)
    {
        if (dtos.Count == 0) return;

        var customerIds = dtos.Select(d => d.CustomerId).Distinct().ToArray();
        var customerInfo = await context.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, FullName = c.Profile.FullName })
            .ToDictionaryAsync(c => c.Id, c => c.FullName, ct);

        var serviceTypeIds = dtos.Select(d => d.ServiceTypeId).Distinct().ToArray();
        var serviceTypeInfo = await context.ServiceTypes
            .AsNoTracking()
            .Where(s => serviceTypeIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.CycleDurationMonths })
            .ToDictionaryAsync(s => s.Id, ct);

        var graveIds = dtos.Where(d => d.GraveId.HasValue).Select(d => d.GraveId!.Value).Distinct().ToArray();
        var graveInfo = graveIds.Length == 0
            ? new Dictionary<long, (string Code, int CotCount)>()
            : (await context.Graves
                .AsNoTracking()
                .Where(g => graveIds.Contains(g.Id))
                .Select(g => new { g.Id, g.GraveCode, g.CotCount })
                .ToArrayAsync(ct))
                .ToDictionary(g => g.Id, g => (Code: g.GraveCode, CotCount: g.CotCount));

        foreach (var d in dtos)
        {
            if (customerInfo.TryGetValue(d.CustomerId, out var cn))
                d.CustomerName = cn;
            if (serviceTypeInfo.TryGetValue(d.ServiceTypeId, out var st))
            {
                d.ServiceTypeName = st.Name;
                d.CycleDurationMonths = st.CycleDurationMonths;
            }
            if (d.GraveId.HasValue && graveInfo.TryGetValue(d.GraveId.Value, out var gi))
            {
                d.GraveCode = gi.Code;
                d.GraveCotCount = gi.CotCount;
            }
        }
    }
}
