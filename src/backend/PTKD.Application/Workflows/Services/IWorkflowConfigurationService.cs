using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Common.Models;
using PTKD.Application.Workflows.DTOs;

namespace PTKD.Application.Workflows.Services;

public interface IWorkflowConfigurationService
{
    Task<BusinessProcessDto[]> GetActiveBusinessProcessesAsync(CancellationToken ct = default);
    Task<PagedResult<WorkflowDefinitionListItemDto>> SearchDefinitionsAsync(WorkflowSearchRequest request, CancellationToken ct = default);
    Task<WorkflowDefinitionDetailDto> CreateDefinitionAsync(CreateWorkflowDefinitionRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowDefinitionDetailDto?> GetDefinitionByIdAsync(long id, CancellationToken ct = default);
    Task<WorkflowDefinitionDetailDto> UpdateDefinitionAsync(long id, UpdateWorkflowDefinitionRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowVersionListItemDto[]> GetVersionsByDefinitionIdAsync(long definitionId, CancellationToken ct = default);
    Task<WorkflowVersionDetailDto> CreateVersionAsync(long definitionId, long actorUserId, CancellationToken ct = default);
    Task<WorkflowVersionDetailDto?> GetVersionByIdAsync(long versionId, CancellationToken ct = default);
    Task DeleteVersionAsync(long versionId, long actorUserId, CancellationToken ct = default);
    Task<WorkflowStepDto> CreateStepAsync(long versionId, CreateWorkflowStepRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowStepDto> UpdateStepAsync(long stepId, UpdateWorkflowStepRequest request, long actorUserId, CancellationToken ct = default);
    Task DeleteStepAsync(long stepId, long actorUserId, CancellationToken ct = default);
    Task<ApproverRuleDto> CreateApproverRuleAsync(long stepId, CreateApproverRuleRequest request, long actorUserId, CancellationToken ct = default);
    Task<ApproverRuleDto> UpdateApproverRuleAsync(long ruleId, CreateApproverRuleRequest request, long actorUserId, CancellationToken ct = default);
    Task DeleteApproverRuleAsync(long ruleId, long actorUserId, CancellationToken ct = default);

    /// <summary>Tạo bản nháp mới SAO CHÉP toàn bộ bước + luật người duyệt + điều kiện của một phiên bản.</summary>
    Task<WorkflowVersionDetailDto> CloneVersionAsync(long sourceVersionId, long actorUserId, CancellationToken ct = default);

    /// <summary>
    /// Danh sách lựa chọn cho ô "giá trị nguồn" tương ứng loại nguồn người duyệt, để admin CHỌN
    /// thay vì phải nhớ và gõ số ID / mã code.
    /// </summary>
    Task<ApproverSourceOptionDto[]> GetApproverSourceOptionsAsync(string sourceType, CancellationToken ct = default);

    /// <summary>Các phiên bản ĐANG HIỆU LỰC có thể gán liên kết cho một mã quy trình.</summary>
    Task<ApproverSourceOptionDto[]> GetBindableVersionsAsync(string processCode, CancellationToken ct = default);

    /// <summary>Ngừng một liên kết (is_active = 0). Trước đây Deactivate() là code chết.</summary>
    Task<WorkflowBindingListItemDto> DeactivateBindingAsync(long bindingId, string targetVersion, long actorUserId, CancellationToken ct = default);
    Task<WorkflowVersionDetailDto> PublishVersionAsync(long versionId, PublishVersionRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowVersionDetailDto> ActivateVersionAsync(long versionId, string targetVersion, long actorUserId, CancellationToken ct = default);
    Task<WorkflowVersionDetailDto> RetireVersionAsync(long versionId, string targetVersion, long actorUserId, CancellationToken ct = default);
    Task<WorkflowBindingListItemDto[]> GetBindingsAsync(string? processCode, CancellationToken ct = default);
    Task<WorkflowBindingListItemDto> CreateBindingAsync(CreateWorkflowBindingRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowBindingListItemDto> UpdateBindingAsync(long bindingId, UpdateWorkflowBindingRequest request, long actorUserId, CancellationToken ct = default);
}
