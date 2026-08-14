using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Domain.Entities;

namespace PTKD.Application.Workflows.Services;

/// <summary>
/// Lớp nền cho các quy trình mà việc "làm gì sau khi duyệt" chỉ là CHUYỂN TRẠNG THÁI một bản ghi.
///
/// Vì sao có lớp này: khảo sát cho thấy các handler dạng này giống nhau tới ~95% — cùng một khuôn
/// mẫu mở context, kiểm loại đối tượng, kiểm idempotency, kiểm trạng thái nguồn, mở giao dịch
/// Serializable, ghi audit, commit. Phần thực sự riêng chỉ 1–3 dòng. Lặp khuôn mẫu ~70 dòng cho
/// mỗi quy trình vừa tốn công vừa dễ sót (đã từng sót đăng ký DI, sót hoàn tác khi từ chối).
///
/// Handler cụ thể nay chỉ KHAI BÁO hợp đồng: mã quy trình, loại đối tượng, cách nạp bản ghi,
/// trạng thái nguồn, các trạng thái coi như đã xong, và phép chuyển trạng thái.
///
/// Vì sao KHÔNG khai báo bằng bảng cấu hình: chuyển trạng thái phải đi qua phương thức nghiệp vụ
/// của thực thể (vd MarkApproved) để giữ các bất biến và dấu vết sửa đổi. Ghi thẳng vào cột trạng
/// thái bằng cấu hình sẽ vượt qua toàn bộ bất biến đó — nới ranh giới quản trị quá mức cần thiết.
/// Hợp đồng vì thế do DEV đăng ký bằng mã nguồn, có kiểm tra kiểu lúc biên dịch.
/// </summary>
public abstract class StatusTransitionExecutionHandler<TEntity> : IWorkflowExecutionHandler
    where TEntity : class
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    protected StatusTransitionExecutionHandler(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    // ── Hợp đồng do handler cụ thể khai báo ──────────────────────────────

    public abstract string ProcessCode { get; }

    /// <summary>Phải khớp WorkflowInstance.BusinessEntityType.</summary>
    protected abstract string BusinessEntityType { get; }

    /// <summary>Trạng thái bắt buộc của bản ghi trước khi thực thi.</summary>
    protected abstract string RequiredStatus { get; }

    /// <summary>Các trạng thái coi như ĐÃ XONG — gặp thì bỏ qua êm (idempotency).</summary>
    protected abstract IReadOnlyCollection<string> AlreadyDoneStatuses { get; }

    protected abstract string ExecutedAuditEventCode { get; }

    protected abstract Task<TEntity?> LoadAsync(IOrganizationDbContext db, long entityId, CancellationToken ct);

    protected abstract string GetStatus(TEntity entity);

    /// <summary>Áp phép chuyển trạng thái khi hồ sơ được DUYỆT (gọi phương thức nghiệp vụ của thực thể).</summary>
    protected abstract void ApplyApproved(TEntity entity, WorkflowInstance instance);

    protected abstract long GetEntityId(TEntity entity);

    // ── Hoàn tác khi TỪ CHỐI (tuỳ chọn) ──────────────────────────────────
    // Chỉ bật ở module CHƯA tự hoàn tác ở tầng service, tránh chạy hai lần.

    protected virtual string? RejectedAuditEventCode => null;

    /// <param name="rejectedByUserId">
    /// NGƯỜI THỰC SỰ TỪ CHỐI (lấy từ nhật ký hành động), không phải người đề xuất — ghi nhầm
    /// thì người đọc nhật ký sẽ tưởng nhân viên tự huỷ yêu cầu của mình.
    /// </param>
    protected virtual void ApplyRejected(TEntity entity, WorkflowInstance instance, string? reason, long rejectedByUserId)
        => throw new NotSupportedException("Handler này chưa khai báo phép chuyển trạng thái khi từ chối.");

    // ── Khuôn mẫu dùng chung ─────────────────────────────────────────────

    public async Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != BusinessEntityType)
            throw new InvalidOperationException(
                $"Sai loại đối tượng cho bộ xử lý '{ProcessCode}': nhận '{instance.BusinessEntityType}', cần '{BusinessEntityType}'.");

        await using var db = _dbContextFactory.CreateDbContext();
        var entity = await LoadAsync(db, instance.BusinessEntityId, ct)
            ?? throw new InvalidOperationException(
                $"Không tìm thấy {BusinessEntityType} #{instance.BusinessEntityId}.");

        var status = GetStatus(entity);

        // Đã xong rồi thì thôi — chạy lại hồ sơ không được làm hỏng dữ liệu.
        if (AlreadyDoneStatuses.Contains(status)) return;

        if (status != RequiredStatus)
            throw new InvalidOperationException(
                $"Không thể thực thi {BusinessEntityType} #{GetEntityId(entity)} khi đang ở trạng thái '{status}' (cần '{RequiredStatus}').");

        ApplyApproved(entity, instance);
        await SaveWithAuditAsync(db, entity, instance, ExecutedAuditEventCode, ct);
    }

    public async Task OnRejectedAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        // Không khai báo hoàn tác → không làm gì (module tự lo ở tầng service).
        if (RejectedAuditEventCode is null) return;
        if (instance.BusinessEntityType != BusinessEntityType) return;

        await using var db = _dbContextFactory.CreateDbContext();
        var entity = await LoadAsync(db, instance.BusinessEntityId, ct);

        // Idempotency: đã rời trạng thái chờ duyệt thì thôi.
        if (entity is null || GetStatus(entity) != RequiredStatus) return;

        var rejectAction = await db.WorkflowActions.AsNoTracking()
            .Where(a => a.WorkflowInstanceId == instance.Id && a.ActionType == "REJECT")
            .OrderByDescending(a => a.Id)
            .Select(a => new { a.Reason, a.ActedBy })
            .FirstOrDefaultAsync(ct);

        var rejectedByUserId = rejectAction?.ActedBy ?? instance.RequesterId;

        ApplyRejected(entity, instance, rejectAction?.Reason, rejectedByUserId);
        await SaveWithAuditAsync(db, entity, instance, RejectedAuditEventCode, ct, rejectedByUserId);
    }

    private async Task SaveWithAuditAsync(
        IOrganizationDbContext db, TEntity entity, WorkflowInstance instance, string eventCode,
        CancellationToken ct, long? actorUserId = null)
    {
        await using var transaction = await db.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await db.SaveChangesAsync(ct);

        var audit = new SecurityAuditEventRecord
        {
            EventCode = eventCode,
            EntityType = BusinessEntityType,
            EntityId = GetEntityId(entity).ToString(),
            Outcome = "SUCCESS",
            // Dùng CorrelationId CỦA HỒ SƠ để nối được chuỗi audit của cả quy trình.
            // (Bản cũ sinh Guid mới mỗi lần nên không lần được dấu vết.)
            CorrelationId = instance.CorrelationId,
            ActorUserId = actorUserId ?? instance.RequesterId,
            AfterStateJson = JsonSerializer.Serialize(new
            {
                EntityId = GetEntityId(entity),
                Status = GetStatus(entity),
                InstanceId = instance.Id
            })
        };
        audit.ThrowIfContainsSensitiveData();
        await _auditWriter.WriteAsync(audit, db.GetDbConnection(), db.GetCurrentDbTransaction()!, ct);

        await transaction.CommitAsync(ct);
    }
}
