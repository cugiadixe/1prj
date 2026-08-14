using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Application.Workflows.DTOs;
using PTKD.Application.Workflows.Services;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/workflows")]
[Authorize]
public class WorkflowConfigurationController : ControllerBase
{
    private readonly IWorkflowConfigurationService _configService;

    public WorkflowConfigurationController(IWorkflowConfigurationService configService)
    {
        _configService = configService;
    }

    [HttpGet("processes")]
    [RequirePermission(PermissionCodes.WorkflowView, PermissionScope.Global)]
    public async Task<IActionResult> GetProcesses(CancellationToken ct)
    {
        var result = await _configService.GetActiveBusinessProcessesAsync(ct);
        return Ok(result);
    }

    [HttpGet("definitions")]
    [RequirePermission(PermissionCodes.WorkflowView, PermissionScope.Global)]
    public async Task<IActionResult> SearchDefinitions([FromQuery] WorkflowSearchRequest request, CancellationToken ct)
    {
        var result = await _configService.SearchDefinitionsAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("definitions")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> CreateDefinition([FromBody] CreateWorkflowDefinitionRequest request, CancellationToken ct)
    {
        var result = await _configService.CreateDefinitionAsync(request, GetActorUserId(), ct);
        return CreatedAtAction(nameof(GetDefinition), new { id = result.Id }, result);
    }

    [HttpGet("definitions/{id}")]
    [RequirePermission(PermissionCodes.WorkflowView, PermissionScope.Global)]
    public async Task<IActionResult> GetDefinition(long id, CancellationToken ct)
    {
        var result = await _configService.GetDefinitionByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("definitions/{id}")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> UpdateDefinition(long id, [FromBody] UpdateWorkflowDefinitionRequest request, CancellationToken ct)
    {
        var result = await _configService.UpdateDefinitionAsync(id, request, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("definitions/{definitionId}/versions")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> CreateVersion(long definitionId, CancellationToken ct)
    {
        var result = await _configService.CreateVersionAsync(definitionId, GetActorUserId(), ct);
        return CreatedAtAction(nameof(GetVersion), new { versionId = result.Id }, result);
    }

    [HttpGet("definitions/{definitionId}/versions")]
    [RequirePermission(PermissionCodes.WorkflowView, PermissionScope.Global)]
    public async Task<IActionResult> GetVersions(long definitionId, CancellationToken ct)
    {
        var result = await _configService.GetVersionsByDefinitionIdAsync(definitionId, ct);
        return Ok(result);
    }

    [HttpGet("versions/{versionId}")]
    [RequirePermission(PermissionCodes.WorkflowView, PermissionScope.Global)]
    public async Task<IActionResult> GetVersion(long versionId, CancellationToken ct)
    {
        var result = await _configService.GetVersionByIdAsync(versionId, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("versions/{versionId}")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> DeleteVersion(long versionId, CancellationToken ct)
    {
        await _configService.DeleteVersionAsync(versionId, GetActorUserId(), ct);
        return NoContent();
    }

    [HttpPost("versions/{versionId}/steps")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> CreateStep(long versionId, [FromBody] CreateWorkflowStepRequest request, CancellationToken ct)
    {
        var result = await _configService.CreateStepAsync(versionId, request, GetActorUserId(), ct);
        return Created($"api/v2/workflows/steps/{result.Id}", result);
    }

    [HttpPut("steps/{stepId}")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> UpdateStep(long stepId, [FromBody] UpdateWorkflowStepRequest request, CancellationToken ct)
    {
        var result = await _configService.UpdateStepAsync(stepId, request, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpDelete("steps/{stepId}")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> DeleteStep(long stepId, CancellationToken ct)
    {
        await _configService.DeleteStepAsync(stepId, GetActorUserId(), ct);
        return NoContent();
    }

    [HttpPost("steps/{stepId}/approver-rules")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> CreateApproverRule(long stepId, [FromBody] CreateApproverRuleRequest request, CancellationToken ct)
    {
        var result = await _configService.CreateApproverRuleAsync(stepId, request, GetActorUserId(), ct);
        return Created($"api/v2/workflows/approver-rules/{result.Id}", result);
    }

    [HttpPut("approver-rules/{ruleId}")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> UpdateApproverRule(long ruleId, [FromBody] CreateApproverRuleRequest request, CancellationToken ct)
    {
        var result = await _configService.UpdateApproverRuleAsync(ruleId, request, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpDelete("approver-rules/{ruleId}")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> DeleteApproverRule(long ruleId, CancellationToken ct)
    {
        await _configService.DeleteApproverRuleAsync(ruleId, GetActorUserId(), ct);
        return NoContent();
    }

    /// <summary>Danh sách lựa chọn cho ô "giá trị nguồn" theo từng loại nguồn người duyệt.</summary>
    [HttpGet("approver-source-options")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> GetApproverSourceOptions([FromQuery] string sourceType, CancellationToken ct)
    {
        var result = await _configService.GetApproverSourceOptionsAsync(sourceType, ct);
        return Ok(result);
    }

    /// <summary>Các phiên bản đang hiệu lực có thể gán liên kết cho một mã quy trình.</summary>
    [HttpGet("bindable-versions")]
    [RequirePermission(PermissionCodes.WorkflowBindProcess, PermissionScope.Global)]
    public async Task<IActionResult> GetBindableVersions([FromQuery] string processCode, CancellationToken ct)
    {
        var result = await _configService.GetBindableVersionsAsync(processCode, ct);
        return Ok(result);
    }

    /// <summary>Ngừng một liên kết quy trình.</summary>
    [HttpPost("bindings/{bindingId}/deactivate")]
    [RequirePermission(PermissionCodes.WorkflowBindProcess, PermissionScope.Global)]
    public async Task<IActionResult> DeactivateBinding(long bindingId, [FromBody] RetireVersionRequest request, CancellationToken ct)
    {
        var result = await _configService.DeactivateBindingAsync(bindingId, request.TargetVersion, GetActorUserId(), ct);
        return Ok(result);
    }

    /// <summary>Nhân bản phiên bản thành bản nháp mới (sao chép bước + luật người duyệt).</summary>
    [HttpPost("versions/{versionId}/clone")]
    [RequirePermission(PermissionCodes.WorkflowConfigManage, PermissionScope.Global)]
    public async Task<IActionResult> CloneVersion(long versionId, CancellationToken ct)
    {
        var result = await _configService.CloneVersionAsync(versionId, GetActorUserId(), ct);
        return Created($"api/v2/workflows/versions/{result.Id}", result);
    }

    [HttpPost("versions/{versionId}/publish")]
    [RequirePermission(PermissionCodes.WorkflowPublish, PermissionScope.Global)]
    public async Task<IActionResult> PublishVersion(long versionId, [FromBody] PublishVersionRequest request, CancellationToken ct)
    {
        var result = await _configService.PublishVersionAsync(versionId, request, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("versions/{versionId}/activate")]
    [RequirePermission(PermissionCodes.WorkflowPublish, PermissionScope.Global)]
    public async Task<IActionResult> ActivateVersion(long versionId, [FromBody] ActivateVersionRequest request, CancellationToken ct)
    {
        var result = await _configService.ActivateVersionAsync(versionId, request.TargetVersion, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("versions/{versionId}/retire")]
    [RequirePermission(PermissionCodes.WorkflowPublish, PermissionScope.Global)]
    public async Task<IActionResult> RetireVersion(long versionId, [FromBody] RetireVersionRequest request, CancellationToken ct)
    {
        var result = await _configService.RetireVersionAsync(versionId, request.TargetVersion, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpGet("bindings")]
    [RequirePermission(PermissionCodes.WorkflowView, PermissionScope.Global)]
    public async Task<IActionResult> GetBindings([FromQuery] string? processCode, CancellationToken ct)
    {
        var result = await _configService.GetBindingsAsync(processCode, ct);
        return Ok(result);
    }

    [HttpPost("bindings")]
    [RequirePermission(PermissionCodes.WorkflowBindProcess, PermissionScope.Global)]
    public async Task<IActionResult> CreateBinding([FromBody] CreateWorkflowBindingRequest request, CancellationToken ct)
    {
        var result = await _configService.CreateBindingAsync(request, GetActorUserId(), ct);
        return Created($"api/v2/workflows/bindings/{result.Id}", result);
    }

    [HttpPut("bindings/{bindingId}")]
    [RequirePermission(PermissionCodes.WorkflowBindProcess, PermissionScope.Global)]
    public async Task<IActionResult> UpdateBinding(long bindingId, [FromBody] UpdateWorkflowBindingRequest request, CancellationToken ct)
    {
        var result = await _configService.UpdateBindingAsync(bindingId, request, GetActorUserId(), ct);
        return Ok(result);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}

public class ActivateVersionRequest
{
    public string TargetVersion { get; set; } = null!;
}

public class RetireVersionRequest
{
    public string TargetVersion { get; set; } = null!;
}
