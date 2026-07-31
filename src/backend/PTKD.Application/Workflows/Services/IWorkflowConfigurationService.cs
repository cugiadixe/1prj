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
    Task<WorkflowVersionDetailDto> PublishVersionAsync(long versionId, PublishVersionRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowVersionDetailDto> ActivateVersionAsync(long versionId, string targetVersion, long actorUserId, CancellationToken ct = default);
    Task<WorkflowVersionDetailDto> RetireVersionAsync(long versionId, string targetVersion, long actorUserId, CancellationToken ct = default);
    Task<WorkflowBindingListItemDto[]> GetBindingsAsync(string? processCode, CancellationToken ct = default);
    Task<WorkflowBindingListItemDto> CreateBindingAsync(CreateWorkflowBindingRequest request, long actorUserId, CancellationToken ct = default);
    Task<WorkflowBindingListItemDto> UpdateBindingAsync(long bindingId, UpdateWorkflowBindingRequest request, long actorUserId, CancellationToken ct = default);
}
