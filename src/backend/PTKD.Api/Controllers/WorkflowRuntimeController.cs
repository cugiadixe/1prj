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
public class WorkflowRuntimeController : ControllerBase
{
    private readonly IWorkflowRuntimeService _runtimeService;

    public WorkflowRuntimeController(IWorkflowRuntimeService runtimeService)
    {
        _runtimeService = runtimeService;
    }

    [HttpPost("instances")]
    public async Task<IActionResult> CreateInstance([FromBody] CreateWorkflowInstanceRequest request, CancellationToken ct)
    {
        var result = await _runtimeService.CreateInstanceAsync(request, GetActorUserId(), ct);
        return CreatedAtAction(nameof(GetInstance), new { instanceId = result.Id }, result);
    }

    [HttpGet("instances/{instanceId}")]
    [RequirePermission(PermissionCodes.WorkflowView, PermissionScope.Global)]
    public async Task<IActionResult> GetInstance(long instanceId, CancellationToken ct)
    {
        var result = await _runtimeService.GetInstanceByIdAsync(instanceId, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("my-approvals")]
    public async Task<IActionResult> GetMyApprovals(CancellationToken ct)
    {
        var result = await _runtimeService.GetMyPendingApprovalsAsync(GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("instances/{instanceId}/steps/{stepId}/approve")]
    public async Task<IActionResult> ApproveStep(long instanceId, long stepId, [FromBody] ApprovalActionRequest request, CancellationToken ct)
    {
        var result = await _runtimeService.ApproveStepAsync(instanceId, stepId, request, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("instances/{instanceId}/steps/{stepId}/return")]
    public async Task<IActionResult> ReturnStep(long instanceId, long stepId, [FromBody] ApprovalActionRequest request, CancellationToken ct)
    {
        var result = await _runtimeService.ReturnStepAsync(instanceId, stepId, request, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("instances/{instanceId}/resubmit")]
    public async Task<IActionResult> ResubmitInstance(long instanceId, [FromBody] ResubmitRequest request, CancellationToken ct)
    {
        var result = await _runtimeService.ResubmitInstanceAsync(instanceId, request.TargetVersion, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("instances/{instanceId}/withdraw")]
    public async Task<IActionResult> WithdrawInstance(long instanceId, [FromBody] WithdrawRequest request, CancellationToken ct)
    {
        var result = await _runtimeService.WithdrawInstanceAsync(instanceId, request.TargetVersion, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("instances/{instanceId}/steps/{stepId}/reassign")]
    [RequirePermission(PermissionCodes.WorkflowReassignPending, PermissionScope.Company)]
    public async Task<IActionResult> ReassignStep(long instanceId, long stepId, [FromBody] ReassignStepRequest request, CancellationToken ct)
    {
        var result = await _runtimeService.ReassignStepAsync(instanceId, stepId, request, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyRequests(CancellationToken ct)
    {
        var result = await _runtimeService.GetMyRequestsAsync(GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpGet("instances/{instanceId}/actions")]
    public async Task<IActionResult> GetInstanceActions(long instanceId, CancellationToken ct)
    {
        var result = await _runtimeService.GetInstanceActionsAsync(instanceId, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("instances/{instanceId}/steps/{stepId}/reject")]
    public async Task<IActionResult> RejectStep(long instanceId, long stepId, [FromBody] ApprovalActionRequest request, CancellationToken ct)
    {
        var result = await _runtimeService.RejectStepAsync(instanceId, stepId, request, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("instances/{instanceId}/retry-execution")]
    [RequirePermission(PermissionCodes.WorkflowRetryExecution, PermissionScope.Global)]
    public async Task<IActionResult> RetryExecution(long instanceId, CancellationToken ct)
    {
        var result = await _runtimeService.RetryExecutionAsync(instanceId, GetActorUserId(), ct);
        return Ok(result);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}

public class ResubmitRequest
{
    public string TargetVersion { get; set; } = null!;
}

public class WithdrawRequest
{
    public string TargetVersion { get; set; } = null!;
}
