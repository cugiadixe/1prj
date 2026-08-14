using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.CarePackages.Handlers;

/// <summary>
/// Duyệt xong yêu cầu bán gói chăm sóc → ĐÃ DUYỆT, rồi mở luôn điều kiện thu tiền
/// (theo quy tắc B2: đủ điều kiện thanh toán khi không cần duyệt, hoặc cần duyệt và đã duyệt).
///
/// Không khai báo hoàn tác khi từ chối: CarePackageRequestService.RejectStepAsync đã tự xử lý
/// ở tầng service.
/// </summary>
public class CarePackageExecutionHandler : StatusTransitionExecutionHandler<CarePackageRequest>
{
    public CarePackageExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
        : base(dbContextFactory, auditWriter) { }

    public override string ProcessCode => "SELL_CARE_PACKAGE";
    protected override string BusinessEntityType => "CarePackageRequest";
    protected override string RequiredStatus => CarePackageRequest.StatusPendingApproval;

    protected override IReadOnlyCollection<string> AlreadyDoneStatuses =>
        [CarePackageRequest.StatusApproved, CarePackageRequest.StatusPaymentEligible];

    protected override string ExecutedAuditEventCode => "SELL_CARE_PACKAGE_WORKFLOW_EXECUTED";

    protected override Task<CarePackageRequest?> LoadAsync(IOrganizationDbContext db, long entityId, CancellationToken ct)
        => db.CarePackageRequests.FirstOrDefaultAsync(c => c.Id == entityId, ct);

    protected override string GetStatus(CarePackageRequest entity) => entity.Status;
    protected override long GetEntityId(CarePackageRequest entity) => entity.Id;

    protected override void ApplyApproved(CarePackageRequest entity, WorkflowInstance instance)
    {
        entity.SetApproved();
        entity.SetPaymentEligible();
    }
}
