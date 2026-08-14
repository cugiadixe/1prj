using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.Cards.Handlers;

/// <summary>
/// Duyệt xong yêu cầu in lại thẻ → chuyển sang ĐÃ DUYỆT để đi tiếp bước thanh toán/in.
///
/// Không khai báo hoàn tác khi từ chối: CardReprintRequestService.RejectStepAsync đã tự xử lý
/// ở tầng service. Bật thêm ở đây sẽ chạy hai lần và ném lỗi ở phép chuyển trạng thái thứ hai.
/// </summary>
public class CardReprintExecutionHandler : StatusTransitionExecutionHandler<CardReprintRequest>
{
    public CardReprintExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
        : base(dbContextFactory, auditWriter) { }

    public override string ProcessCode => "CARD_REPRINT";
    protected override string BusinessEntityType => "CardReprintRequest";
    protected override string RequiredStatus => CardReprintRequest.StatusPendingApproval;
    protected override IReadOnlyCollection<string> AlreadyDoneStatuses => [CardReprintRequest.StatusApproved];
    protected override string ExecutedAuditEventCode => "CARD_REPRINT_WORKFLOW_EXECUTED";

    protected override Task<CardReprintRequest?> LoadAsync(IOrganizationDbContext db, long entityId, CancellationToken ct)
        => db.CardReprintRequests.FirstOrDefaultAsync(c => c.Id == entityId, ct);

    protected override string GetStatus(CardReprintRequest entity) => entity.Status;
    protected override long GetEntityId(CardReprintRequest entity) => entity.Id;

    protected override void ApplyApproved(CardReprintRequest entity, WorkflowInstance instance)
        => entity.SetApproved();
}
