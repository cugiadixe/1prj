using System;
using System.Data.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Domain.Entities;

namespace PTKD.Application.Customers.Handlers;

/// <summary>
/// Lõi thực thi gộp khách hàng, DÙNG CHUNG cho hai đường:
///   1) Qua workflow: CustomerMergeExecutionHandler gọi sau khi hồ sơ duyệt xong.
///   2) Tự duyệt: CustomerMergeService gọi trực tiếp khi người gửi có quyền
///      CUSTOMER_MERGE_EXECUTE toàn cục (admin full quyền) — tự tạo + tự duyệt.
///
/// Việc gộp: đánh dấu hồ sơ NGUỒN = MERGED (trỏ survivor về ĐÍCH) và DỒN toàn bộ dữ liệu con của
/// nguồn sang đích (mộ, cốt, liên hệ khẩn, gói/dịch vụ, thanh toán, quan hệ, thẻ tag, context công
/// ty), hoà giải ràng buộc UNIQUE. Không thể hoàn tác.
/// </summary>
public class CustomerMergeExecutor
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public CustomerMergeExecutor(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    /// <summary>
    /// Thực thi gộp cho một yêu cầu. Chấp nhận trạng thái DRAFT (tự duyệt) / SUBMITTED / APPROVED.
    /// Idempotent: đã EXECUTED thì bỏ qua; REJECTED/WITHDRAWN thì không đụng dữ liệu.
    /// <paramref name="correlationId"/> để gắn dấu vết kiểm toán với phiên duyệt (nếu có).
    /// </summary>
    public async Task ExecuteAsync(Guid mergeRequestId, long actorId, Guid? correlationId, CancellationToken ct = default)
    {
        // Bọc trong execution strategy (DeadlockRetryPolicy) — bắt buộc khi có retry policy thì mới
        // được mở transaction. Mỗi lần thử dùng CONTEXT MỚI để tránh dữ liệu theo dõi bị cũ khi retry.
        await using var probeContext = _dbContextFactory.CreateDbContext();
        var strategy = probeContext.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var dbContext = _dbContextFactory.CreateDbContext();

                var mergeRequest = await dbContext.CustomerMergeRequests
                    .FirstOrDefaultAsync(c => c.Id == mergeRequestId, ct)
                    ?? throw new InvalidOperationException("Customer merge request not found.");

                if (mergeRequest.RequestStatus == "EXECUTED")
                    return;
                if (mergeRequest.RequestStatus == "REJECTED" || mergeRequest.RequestStatus == "WITHDRAWN")
                    return;
                if (mergeRequest.RequestStatus is not ("DRAFT" or "SUBMITTED" or "APPROVED"))
                    throw new InvalidOperationException($"Cannot execute request in state {mergeRequest.RequestStatus}.");

                var sourceCustomer = await dbContext.Customers
                    .FirstOrDefaultAsync(c => c.Id == mergeRequest.SourceCustomerId, ct);
                var targetCustomer = await dbContext.Customers
                    .FirstOrDefaultAsync(c => c.Id == mergeRequest.TargetCustomerId, ct);

                if (sourceCustomer == null || targetCustomer == null)
                    throw new InvalidOperationException("Source or target customer not found.");
                if (targetCustomer.CustomerStatus == "MERGED")
                    throw new InvalidOperationException("Target customer is already merged into another record.");

                // Chống ghi đè: khách bị sửa kể từ lúc tạo yêu cầu thì dừng, tránh dồn nhầm dữ liệu.
                if (!Convert.ToBase64String(sourceCustomer.RowVersion).Equals(Convert.ToBase64String(mergeRequest.SourceRowVersionSnapshot)))
                    throw new InvalidOperationException("Concurrency conflict: Source customer has been modified since the request was created.");
                if (!Convert.ToBase64String(targetCustomer.RowVersion).Equals(Convert.ToBase64String(mergeRequest.TargetRowVersionSnapshot)))
                    throw new InvalidOperationException("Concurrency conflict: Target customer has been modified since the request was created.");

                await using var transaction = await dbContext.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

                // 1) Dồn toàn bộ dữ liệu con nguồn→đích (SQL tập hợp, hoà giải UNIQUE) — TRƯỚC khi lật MERGED.
                await ReassignChildDataAsync(dbContext, mergeRequest.SourceCustomerId, mergeRequest.TargetCustomerId, ct);

                // 2) Lật nguồn = MERGED, trỏ survivor về đích.
                sourceCustomer.SetStatus("MERGED", actorId, targetCustomer.Id);

                // 3) Đóng yêu cầu + ghi lịch sử gộp.
                mergeRequest.SetExecuted();
                dbContext.CustomerMergeHistory.Add(new CustomerMergeHistory(
                    mergeRequest.Id, sourceCustomer.Id, targetCustomer.Id, "EXECUTED", actorId, mergeRequest.SurvivorshipPayload));

                await dbContext.SaveChangesAsync(ct);

                var audit = new SecurityAuditEventRecord
                {
                    EventCode = "CUSTOMER_MERGE_EXECUTED",
                    EntityType = "Customer",
                    EntityId = targetCustomer.Id.ToString(),
                    Outcome = "SUCCESS",
                    CorrelationId = correlationId ?? Guid.NewGuid(),
                    ActorUserId = actorId,
                    AfterStateJson = JsonSerializer.Serialize(new { SourceId = sourceCustomer.Id, TargetId = targetCustomer.Id, RequestId = mergeRequest.Id })
                };
                audit.ThrowIfContainsSensitiveData();
                await _auditWriter.WriteAsync(audit, dbContext.GetDbConnection(), dbContext.GetCurrentDbTransaction()!, ct);

                await transaction.CommitAsync(ct);
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            await MarkRejectedAsync(mergeRequestId, ct);
            throw new InvalidOperationException("Concurrency conflict during merge execution.");
        }
    }

    /// <summary>Đưa yêu cầu gộp ra khỏi trạng thái chờ (SUBMITTED/APPROVED → REJECTED) khi hồ sơ bị từ chối/lỗi.</summary>
    public async Task MarkRejectedAsync(Guid mergeRequestId, CancellationToken ct = default)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var mr = await dbContext.CustomerMergeRequests.FirstOrDefaultAsync(c => c.Id == mergeRequestId, ct);
        if (mr != null && (mr.RequestStatus == "SUBMITTED" || mr.RequestStatus == "APPROVED"))
        {
            mr.SetRejected();
            await dbContext.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Dời mọi bản ghi con trỏ về khách NGUỒN sang khách ĐÍCH. Với bảng có UNIQUE liên quan khách
    /// hàng thì XOÁ bản trùng của nguồn trước khi dời. Dùng SQL tập hợp vì các entity đặt setter
    /// private (không sửa trực tiếp qua EF) và số bản ghi nhỏ.
    /// </summary>
    private static async Task ReassignChildDataAsync(IOrganizationDbContext dbContext, long src, long tgt, CancellationToken ct)
    {
        const string sql = @"
-- Vài bảng có filtered index (VD Grave_Occupants) đòi QUOTED_IDENTIFIER ON khi ghi. SqlClient mặc
-- định đã ON, nhưng khai tường minh để không phụ thuộc cấu hình kết nối.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Context công ty: UNIQUE(customer_id, company_id) → bỏ context nguồn khi đích đã có cùng công ty.
DELETE FROM dbo.Customer_Company_Contexts
 WHERE customer_id=@src AND company_id IN (SELECT company_id FROM dbo.Customer_Company_Contexts WHERE customer_id=@tgt);
UPDATE dbo.Customer_Company_Contexts SET customer_id=@tgt WHERE customer_id=@src;

-- Thẻ tag: UNIQUE(customer_id, tag_id) → bỏ tag nguồn khi đích đã có cùng tag.
DELETE FROM dbo.Customer_Tags
 WHERE customer_id=@src AND tag_id IN (SELECT tag_id FROM dbo.Customer_Tags WHERE customer_id=@tgt);
UPDATE dbo.Customer_Tags SET customer_id=@tgt WHERE customer_id=@src;

-- Quan hệ: UNIQUE(from,to) + CHECK từ<>đến. Bỏ cạnh trực tiếp nguồn↔đích (sẽ thành tự-vòng),
-- khử trùng theo cả hai chiều rồi mới dời điểm cuối.
DELETE FROM dbo.Customer_Relationships
 WHERE (from_customer_id=@src AND to_customer_id=@tgt) OR (from_customer_id=@tgt AND to_customer_id=@src);
DELETE cr FROM dbo.Customer_Relationships cr
 WHERE cr.from_customer_id=@src
   AND EXISTS (SELECT 1 FROM dbo.Customer_Relationships x WHERE x.from_customer_id=@tgt AND x.to_customer_id=cr.to_customer_id);
UPDATE dbo.Customer_Relationships SET from_customer_id=@tgt WHERE from_customer_id=@src;
DELETE cr FROM dbo.Customer_Relationships cr
 WHERE cr.to_customer_id=@src
   AND EXISTS (SELECT 1 FROM dbo.Customer_Relationships x WHERE x.to_customer_id=@tgt AND x.from_customer_id=cr.from_customer_id);
UPDATE dbo.Customer_Relationships SET to_customer_id=@tgt WHERE to_customer_id=@src;

-- Dời thẳng (không ràng buộc UNIQUE theo khách): mộ sở hữu, cốt, liên hệ khẩn, gói/dịch vụ, thanh toán.
UPDATE dbo.Graves                   SET owner_customer_id=@tgt    WHERE owner_customer_id=@src;
UPDATE dbo.Grave_Occupants          SET deceased_customer_id=@tgt WHERE deceased_customer_id=@src;
UPDATE dbo.Grave_Emergency_Contacts SET contact_customer_id=@tgt  WHERE contact_customer_id=@src;
UPDATE dbo.Customer_Care_Packages   SET customer_id=@tgt          WHERE customer_id=@src;
UPDATE dbo.Care_Package_Requests    SET customer_id=@tgt          WHERE customer_id=@src;
UPDATE dbo.Services                 SET customer_id=@tgt          WHERE customer_id=@src;
UPDATE dbo.Payment_Transactions     SET customer_id=@tgt          WHERE customer_id=@src;
";

        using var cmd = dbContext.GetDbConnection().CreateCommand();
        cmd.Transaction = dbContext.GetCurrentDbTransaction();
        cmd.CommandText = sql;
        AddParam(cmd, "@src", src);
        AddParam(cmd, "@tgt", tgt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void AddParam(DbCommand cmd, string name, long value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
