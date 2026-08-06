using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Workflows.DTOs;

namespace PTKD.Application.Workflows.Services;

public interface IWorkflowRuntimeService
{
    Task<WorkflowInstanceDto> CreateInstanceAsync(CreateWorkflowInstanceRequest request, long requesterId, CancellationToken ct = default);
    Task<WorkflowInstanceDto?> GetInstanceByIdAsync(long instanceId, CancellationToken ct = default);
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
}
