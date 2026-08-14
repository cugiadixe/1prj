using PTKD.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Workflows.DTOs;

namespace PTKD.Application.Workflows.Services;

public interface IWorkflowRuntimeService
{
    Task<WorkflowInstanceDto> CreateInstanceAsync(CreateWorkflowInstanceRequest request, long requesterId, CancellationToken ct = default);
    Task<WorkflowInstanceDto?> GetInstanceByIdAsync(long instanceId, long actorUserId, CancellationToken ct = default);
    Task<MyApprovalItemDto[]> GetMyPendingApprovalsAsync(long userId, CancellationToken ct = default);
    Task<WorkflowInstanceDto> ApproveStepAsync(long instanceId, long stepId, ApprovalActionRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowInstanceDto> ReturnStepAsync(long instanceId, long stepId, ApprovalActionRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowInstanceDto> ResubmitInstanceAsync(long instanceId, string targetVersion, long requesterId, CancellationToken ct = default);
    Task<WorkflowInstanceDto> WithdrawInstanceAsync(long instanceId, string targetVersion, long requesterId, CancellationToken ct = default);
    Task<WorkflowInstanceDto> ReassignStepAsync(long instanceId, long stepId, ReassignStepRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowInstanceDto[]> GetMyRequestsAsync(long requesterId, CancellationToken ct = default);
    Task<WorkflowActionDto[]> GetInstanceActionsAsync(long instanceId, long userId, CancellationToken ct = default);
    Task<WorkflowInstanceDto> RejectStepAsync(long instanceId, long stepId, ApprovalActionRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowInstanceDto> RetryExecutionAsync(long instanceId, long actorUserId, CancellationToken ct = default);

    /// <summary>
    /// Tra cứu hồ sơ cho QUẢN TRỊ (cần quyền WORKFLOW_VIEW). Trước đây admin chỉ mở được hồ sơ
    /// nếu đã biết ID, nên hồ sơ Thất bại nằm im không ai thấy để chạy lại.
    /// </summary>
    Task<PagedResult<WorkflowInstanceDto>> SearchInstancesAsync(WorkflowInstanceSearchRequest request, long actorUserId, CancellationToken ct = default);

    /// <summary>
    /// Hồ sơ với dữ liệu này có phải qua phê duyệt không — trả lời bằng CẤU HÌNH (liên kết +
    /// điều kiện) chứ không phải luật cứng trong code. Chỉ đọc, không tạo gì.
    ///
    /// Trả về <c>null</c> khi quy trình CHƯA có liên kết nào: lúc đó engine không có cơ sở để
    /// kết luận, module gọi phải tự quyết (thường là giữ luật cũ làm lưới an toàn) — tuyệt đối
    /// không được hiểu "không có liên kết" thành "không cần duyệt".
    /// </summary>
    Task<bool?> IsApprovalRequiredAsync(string processCode, long? companyId, string? payloadJson, CancellationToken ct = default);
}
