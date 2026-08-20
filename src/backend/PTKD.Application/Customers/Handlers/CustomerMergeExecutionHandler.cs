using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;

namespace PTKD.Application.Customers.Handlers;

/// <summary>
/// Bộ xử lý workflow cho quy trình gộp khách hàng trùng: được engine gọi sau khi hồ sơ duyệt xong.
/// Logic gộp thật nằm ở <see cref="CustomerMergeExecutor"/> (dùng chung với đường tự duyệt của admin).
///
/// Định danh yêu cầu gộp (Guid) lấy từ PayloadJson vì WorkflowInstance.BusinessEntityId là long
/// không chứa được Guid (BusinessEntityId chỉ dùng làm "mỏ neo" hiển thị = TargetCustomerId).
/// </summary>
public class CustomerMergeExecutionHandler : IWorkflowExecutionHandler
{
    private readonly CustomerMergeExecutor _executor;

    public string ProcessCode => "CUSTOMER_MERGE_DUPLICATE";

    public CustomerMergeExecutionHandler(CustomerMergeExecutor executor)
    {
        _executor = executor;
    }

    public async Task ExecuteAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != "CustomerMergeRequest")
            throw new InvalidOperationException("Invalid business entity type for this handler.");

        var mergeRequestId = ParseMergeRequestId(instance);
        await _executor.ExecuteAsync(mergeRequestId, instance.RequesterId, instance.CorrelationId, ct);
    }

    public async Task OnRejectedAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        if (instance.BusinessEntityType != "CustomerMergeRequest")
            return;

        var mergeRequestId = ParseMergeRequestId(instance);
        await _executor.MarkRejectedAsync(mergeRequestId, ct);
    }

    private static Guid ParseMergeRequestId(WorkflowInstance instance)
    {
        try
        {
            using var doc = JsonDocument.Parse(instance.PayloadJson);
            if (doc.RootElement.TryGetProperty("MergeRequestId", out var el) && Guid.TryParse(el.GetString(), out var id))
                return id;
        }
        catch { /* rơi xuống lỗi rõ ràng bên dưới */ }

        throw new InvalidOperationException("Merge request id missing from workflow payload.");
    }
}
