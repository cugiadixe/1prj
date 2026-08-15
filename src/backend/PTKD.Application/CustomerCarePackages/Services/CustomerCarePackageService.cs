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
using PTKD.Application.Security.Authorization;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Workflows.DTOs;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.CustomerCarePackages.Services;

public class CustomerCarePackageService : ICustomerCarePackageService
{
    private const string AssignProcessCode = "ASSIGN_CARE_PACKAGE";

    private const string ViewPermission = "CUSTOMER_CARE_PACKAGE_VIEW";
    private const string ManagePermission = "CUSTOMER_CARE_PACKAGE_MANAGE";

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;
    private readonly IWorkflowRuntimeService _workflowRuntimeService;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public CustomerCarePackageService(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter,
        IWorkflowRuntimeService workflowRuntimeService,
        IPermissionEvaluator permissionEvaluator)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
        _workflowRuntimeService = workflowRuntimeService;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<CustomerCarePackageDto[]> ListByCustomerAsync(long customerId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        if (!await CustomerCompanyScope.CanAccessCustomerAsync(context, customerId, scope, ct))
            return Array.Empty<CustomerCarePackageDto>();

        var items = await context.CustomerCarePackages
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.Id)
            .Select(MapExpression())
            .ToArrayAsync(ct);
        await EnrichAsync(context, items, ct);
        return items;
    }

    public async Task<CustomerCarePackageDto[]> ListByGraveAsync(long graveId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        var items = await context.CustomerCarePackages
            .AsNoTracking()
            .Where(p => p.GraveId == graveId)
            .OrderByDescending(p => p.Id)
            .Select(MapExpression())
            .ToArrayAsync(ct);

        // Một mộ có thể mang gói của nhiều khách, nên lọc theo TỪNG khách chứ không lọc theo mộ.
        // Mộ hiện chưa có chiều công ty trong dữ liệu, đó là việc riêng chưa làm được ở đây.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        var accessible = await CustomerCompanyScope.FilterAccessibleCustomerIdsAsync(
            context, items.Select(i => i.CustomerId).Distinct().ToList(), scope, ct);

        items = items.Where(i => accessible.Contains(i.CustomerId)).ToArray();

        await EnrichAsync(context, items, ct);
        return items;
    }

    public async Task<CustomerCarePackageDto> CreateAsync(long? companyId, CreateCustomerCarePackageRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (request.CotCount <= 0)
            throw new BusinessRuleValidationException("CCP_INVALID_COT_COUNT", "Số cốt phải lớn hơn 0.");

        // CHÍNH SÁCH: gán gói là quy trình BẮT BUỘC phê duyệt. Thiếu cấu hình thì CHẶN,
        // không được âm thầm bỏ qua bước duyệt (trước đây gán thẳng — rủi ro bỏ sót phê duyệt).
        if (companyId is not > 0)
            throw new BusinessRuleValidationException(
                "CCP_COMPANY_CONTEXT_REQUIRED",
                "Chưa xác định công ty làm việc nên không xác định được quy trình phê duyệt. Vui lòng chọn công ty rồi thử lại.");

        if (!await HasActiveAssignBindingAsync(companyId.Value, ct))
            throw new BusinessRuleValidationException(
                "CCP_APPROVAL_NOT_CONFIGURED",
                "Quy trình phê duyệt gán gói dịch vụ chưa được cấu hình cho công ty này. Vui lòng liên hệ quản trị để khai báo liên kết quy trình.");

        const bool requiresApproval = true;

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
        catch (BusinessRuleValidationException ex) when (ex.ErrorCode == "WF_ONLY_REQUESTER_IS_APPROVER")
        {
            // CA B — người đề xuất chính là người duyệt duy nhất (vd trưởng phòng tự tạo).
            // Đây là quy tắc nghiệp vụ đã duyệt: TỰ ĐỘNG DUYỆT nhưng phải ghi dấu rõ ràng.
            await AutoApproveAsync(packageId, actorUserId, ex.ErrorCode, ct);
        }
        catch (Exception)
        {
            // CA A/C — thiếu cấu hình hoặc không xác định được người duyệt: KHÔNG tự duyệt.
            // Gói đã lỡ được tạo ở giao dịch trước nên phải hủy để không kẹt "Chờ duyệt" mồ côi.
            // Dùng CancellationToken.None: nếu request bị hủy/timeout thì việc dọn dẹp VẪN phải chạy,
            // nếu không gói sẽ nằm lại vĩnh viễn — đúng cái đang muốn tránh.
            await CancelOrphanAsync(packageId, actorUserId, CancellationToken.None);
            throw;
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

    /// <summary>
    /// Bù trừ: hủy gói vừa tạo khi không sinh được hồ sơ phê duyệt. Không có bước này thì gói
    /// nằm lại ở "Chờ duyệt" mà không có hồ sơ nào — không ai duyệt được, cũng không ai hủy được.
    /// </summary>
    private async Task CancelOrphanAsync(long packageId, long actorUserId, CancellationToken ct)
    {
        try
        {
            await using var tempContext = _dbContextFactory.CreateDbContext();
            var strategy = tempContext.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var context = _dbContextFactory.CreateDbContext();
                await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
                var package = await context.CustomerCarePackages.FirstOrDefaultAsync(p => p.Id == packageId, ct);
                if (package != null && package.Status == CustomerCarePackage.StatusPendingApproval)
                {
                    package.Cancel(actorUserId);
                    await context.SaveChangesAsync(ct);
                    await WriteAuditAsync(context, "CARE_PACKAGE_CANCELLED_NO_WORKFLOW", packageId, actorUserId,
                        new { PackageId = packageId, reason = "Không tạo được hồ sơ phê duyệt nên hủy gói vừa tạo." }, ct);
                }
                await transaction.CommitAsync(ct);
            });
        }
        catch (Exception)
        {
            // Bù trừ thất bại không được che mất lỗi gốc — lỗi gốc mới là thứ người dùng cần thấy.
        }
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
                    new { package.Id, reasonCode, reason = "Người đề xuất cũng là người duyệt duy nhất — tự động duyệt." }, ct);
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

            // Bản ghi đích phải thuộc công ty người gọi được phép. Ba kiểm tra nghiệp vụ bên dưới
            // (mộ thuộc sở hữu khách, số cốt khớp, không trùng gói) đều là quan hệ NỘI BỘ giữa
            // gói và mộ, không ràng buộc gì với công ty của người gọi — nên thiếu chốt này thì
            // gán được mộ cho gói của công ty khác chỉ bằng cách đoán id.
            var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ManagePermission, ct);
            await CustomerCompanyScope.EnsureCustomerAccessibleAsync(
                context, package.CustomerId, scope, "CCP_COMPANY_FORBIDDEN", ct);

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

            // Không có chốt này thì huỷ được gói ĐANG HIỆU LỰC của công ty khác chỉ bằng cách
            // đoán id — và Cancel() bên dưới không có chốt trạng thái nào chặn lại.
            var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ManagePermission, ct);
            await CustomerCompanyScope.EnsureCustomerAccessibleAsync(
                context, package.CustomerId, scope, "CCP_COMPANY_FORBIDDEN", ct);

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
