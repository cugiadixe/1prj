using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Api.Controllers.Security;

/// <summary>
/// Manages role assignments for a specific user.
/// POST is idempotent on exact active duplicate (200/204), 409 on overlap (OD-D-B-06).
/// DELETE deactivates — no hard delete (OD-D-B-07).
/// Company-scoped roles check user has active company assignment (OD-D-B-15).
/// </summary>
[ApiController]
[Authorize]
[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]
[Route("api/v2/security/users/{userId:long}/role-assignments")]
public sealed class UserRoleAssignmentsController : ControllerBase
{
    private const string RequiredPermission = PermissionCodes.SecurityAdminManage;

    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ISecurityAdminService _service;

    public UserRoleAssignmentsController(IPermissionEvaluator permissionEvaluator, ISecurityAdminService service)
    {
        _permissionEvaluator = permissionEvaluator;
        _service = service;
    }

    /// <summary>GET /api/v2/security/users/{userId}/role-assignments</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserRoleAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(long userId, CancellationToken ct)
    {
        return Ok(await _service.ListUserRoleAssignmentsAsync(userId, ct));
    }

    /// <summary>
    /// POST /api/v2/security/users/{userId}/role-assignments
    /// Returns 200 on exact active duplicate (idempotent, OD-D-B-06).
    /// Returns 201 on successful creation.
    /// Returns 409 on temporal overlap.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserRoleAssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UserRoleAssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assign(long userId, [FromBody] CreateUserRoleAssignmentRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        var (assignment, wasIdempotent) = await _service.AssignRoleAsync(actor, userId, request, ct);

        if (wasIdempotent)
            return Ok(assignment);

        return CreatedAtAction(nameof(List), new { userId }, assignment);
    }

    /// <summary>
    /// DELETE /api/v2/security/users/{userId}/role-assignments/{id}
    /// Deactivates — soft delete (OD-D-B-07).
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(long userId, long id, [FromBody] DeactivateAssignmentRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        await _service.DeactivateUserRoleAssignmentAsync(actor, userId, id, request, ct);
        return NoContent();
    }
}
