using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.CustomerCarePackages.Handlers;

/// <summary>
/// Quy trình "Gán gói dịch vụ cho khách" (ASSIGN_CARE_PACKAGE).
/// Duyệt xong: PENDING_APPROVAL → PENDING_GRAVE, lúc này gói mới hiện ra để gán vào mộ.
/// Bị từ chối: đưa gói ra khỏi trạng thái chờ duyệt, nếu không gói kẹt "Chờ duyệt" vĩnh viễn.
/// </summary>
public class AssignCarePackageExecutionHandler : StatusTransitionExecutionHandler<CustomerCarePackage>
{
    public AssignCarePackageExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
        : base(dbContextFactory, auditWriter) { }

    public override string ProcessCode => "ASSIGN_CARE_PACKAGE";
    protected override string BusinessEntityType => "CustomerCarePackage";
    protected override string RequiredStatus => CustomerCarePackage.StatusPendingApproval;

    // Mọi trạng thái khác PENDING_APPROVAL đều coi là đã xử lý xong — giữ đúng hành vi khoan dung
    // trước đây (gói bị huỷ/từ chối trong lúc chờ thì bỏ qua êm, không đánh dấu hồ sơ Thất bại).
    protected override IReadOnlyCollection<string> AlreadyDoneStatuses =>
    [
        CustomerCarePackage.StatusPendingGrave,
        CustomerCarePackage.StatusActive,
        CustomerCarePackage.StatusExpired,
        CustomerCarePackage.StatusCancelled,
    ];

    protected override string ExecutedAuditEventCode => "CARE_PACKAGE_APPROVAL_EXECUTED";
    protected override string? RejectedAuditEventCode => "CARE_PACKAGE_APPROVAL_REJECTED";

    protected override Task<CustomerCarePackage?> LoadAsync(IOrganizationDbContext db, long entityId, CancellationToken ct)
        => db.CustomerCarePackages.FirstOrDefaultAsync(p => p.Id == entityId, ct);

    protected override string GetStatus(CustomerCarePackage entity) => entity.Status;
    protected override long GetEntityId(CustomerCarePackage entity) => entity.Id;

    protected override void ApplyApproved(CustomerCarePackage entity, WorkflowInstance instance)
        => entity.MarkApproved(instance.RequesterId);

    protected override void ApplyRejected(CustomerCarePackage entity, WorkflowInstance instance, string? reason, long rejectedByUserId)
        => entity.MarkRejected(rejectedByUserId, reason);
}
