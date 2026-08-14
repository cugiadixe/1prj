using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.CustomerCarePackages.Handlers;

/// <summary>
/// Bộ xử lý khi quy trình "Gán gói dịch vụ cho khách" (ASSIGN_CARE_PACKAGE) được duyệt xong.
/// Chuyển gói từ PENDING_APPROVAL sang PENDING_GRAVE — lúc này gói mới thực sự hiện cho khách
/// để gán vào mộ (luồng c của yêu cầu).
/// </summary>
public class AssignCarePackageExecutionHandler : IWorkflowExecutionHandler
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public string ProcessCode => "ASSIGN_CARE_PACKAGE";

    public AssignCarePackageExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != "CustomerCarePackage")
            throw new InvalidOperationException("Invalid business entity type for this handler.");

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var package = await dbContext.CustomerCarePackages
            .FirstOrDefaultAsync(p => p.Id == instance.BusinessEntityId, ct);

        if (package == null)
            throw new InvalidOperationException("Customer care package not found.");

        // Idempotency: đã qua PENDING_APPROVAL thì thôi.
        if (package.Status != CustomerCarePackage.StatusPendingApproval)
            return;

        // Người duyệt = người đóng bước cuối; ở đây dùng requester làm actor ghi nhận thay đổi
        // trạng thái (WorkflowAction đã ghi ai thực sự duyệt).
        package.MarkApproved(instance.RequesterId);

        await using var transaction = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await dbContext.SaveChangesAsync(ct);

        var audit = new SecurityAuditEventRecord
        {
            EventCode = "CARE_PACKAGE_APPROVAL_EXECUTED",
            EntityType = "CustomerCarePackage",
            EntityId = package.Id.ToString(),
            Outcome = "SUCCESS",
            CorrelationId = instance.CorrelationId,
            ActorUserId = instance.RequesterId,
            AfterStateJson = JsonSerializer.Serialize(new { PackageId = package.Id, Status = package.Status, InstanceId = instance.Id })
        };
        audit.ThrowIfContainsSensitiveData();
        await _auditWriter.WriteAsync(audit, dbContext.GetDbConnection(), dbContext.GetCurrentDbTransaction()!, ct);

        await transaction.CommitAsync(ct);
    }

    /// <summary>
    /// Trưởng phòng TỪ CHỐI: đưa gói ra khỏi trạng thái chờ duyệt (nếu không, gói kẹt
    /// "Chờ duyệt" vĩnh viễn — khách không dùng được mà cũng không ai xử lý được).
    /// </summary>
    public async Task OnRejectedAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != "CustomerCarePackage")
            return;

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var package = await dbContext.CustomerCarePackages
            .FirstOrDefaultAsync(p => p.Id == instance.BusinessEntityId, ct);

        // Idempotency: gói đã rời trạng thái chờ duyệt thì thôi.
        if (package == null || package.Status != CustomerCarePackage.StatusPendingApproval)
            return;

        // Lấy cả LÝ DO và NGƯỜI thực sự từ chối — ghi người đề xuất vào đây là sai lịch sử,
        // người đọc nhật ký sẽ tưởng nhân viên tự hủy yêu cầu của mình.
        var rejectAction = await dbContext.WorkflowActions.AsNoTracking()
            .Where(a => a.WorkflowInstanceId == instance.Id && a.ActionType == "REJECT")
            .OrderByDescending(a => a.Id)
            .Select(a => new { a.Reason, a.ActedBy })
            .FirstOrDefaultAsync(ct);

        var reason = rejectAction?.Reason;
        var rejectedByUserId = rejectAction?.ActedBy ?? instance.RequesterId;

        package.MarkRejected(rejectedByUserId, reason);

        await using var transaction = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await dbContext.SaveChangesAsync(ct);

        var audit = new SecurityAuditEventRecord
        {
            EventCode = "CARE_PACKAGE_APPROVAL_REJECTED",
            EntityType = "CustomerCarePackage",
            EntityId = package.Id.ToString(),
            Outcome = "SUCCESS",
            CorrelationId = instance.CorrelationId,
            ActorUserId = rejectedByUserId,
            AfterStateJson = JsonSerializer.Serialize(new { PackageId = package.Id, Status = package.Status, InstanceId = instance.Id })
        };
        audit.ThrowIfContainsSensitiveData();
        await _auditWriter.WriteAsync(audit, dbContext.GetDbConnection(), dbContext.GetCurrentDbTransaction()!, ct);

        await transaction.CommitAsync(ct);
    }
}
