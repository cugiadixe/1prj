using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Api.Controllers.Security;

/// <summary>
/// Manages individual permission grants (ALLOW/DENY) for a specific user.
/// POST is idempotent on exact active duplicate (200/204), 409 on overlap (OD-D-B-06).
/// DELETE deactivates — no hard delete (OD-D-B-07).
/// Company-scoped permissions check user has active company assignment (OD-D-B-15).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v2/security/users/{userId:long}/individual-permissions")]
public sealed class UserIndividualPermissionsController : ControllerBase
{
    private const string RequiredPermission = PermissionCodes.SecurityAdminManage;

    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ISecurityAdminService _service;

    public UserIndividualPermissionsController(IPermissionEvaluator permissionEvaluator, ISecurityAdminService service)
    {
        _permissionEvaluator = permissionEvaluator;
        _service = service;
    }

    /// <summary>GET /api/v2/security/users/{userId}/individual-permissions</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserIndividualPermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(long userId, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);
        await SecurityControllerHelper.EnforcePermissionAsync(_permissionEvaluator, actor, RequiredPermission, null, ct);

        return Ok(await _service.ListUserIndividualPermissionsAsync(userId, ct));
    }

    /// <summary>
    /// POST /api/v2/security/users/{userId}/individual-permissions
    /// Returns 200 on exact active duplicate (idempotent, OD-D-B-06).
    /// Returns 201 on successful creation.
    /// Returns 409 on temporal overlap.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserIndividualPermissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UserIndividualPermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Grant(long userId, [FromBody] CreateUserIndividualPermissionRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);
        await SecurityControllerHelper.EnforcePermissionAsync(_permissionEvaluator, actor, RequiredPermission, null, ct);

        var (permission, wasIdempotent) = await _service.GrantIndividualPermissionAsync(actor, userId, request, ct);

        if (wasIdempotent)
            return Ok(permission);

        return CreatedAtAction(nameof(List), new { userId }, permission);
    }

    /// <summary>
    /// DELETE /api/v2/security/users/{userId}/individual-permissions/{id}
    /// Deactivates — soft delete (OD-D-B-07).
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(long userId, long id, [FromBody] DeactivateAssignmentRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);
        await SecurityControllerHelper.EnforcePermissionAsync(_permissionEvaluator, actor, RequiredPermission, null, ct);

        await _service.DeactivateIndividualPermissionAsync(actor, userId, id, request, ct);
        return NoContent();
    }
}
