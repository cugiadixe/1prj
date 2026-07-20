using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Api.Controllers.Security;

/// <summary>
/// CRUD for Roles and their permission assignments.
/// All mutations require SECURITY_ADMIN_MANAGE (OD-D-B-02, OD-D-B-03).
/// Deactivation is soft (OD-D-B-07). Mutations increment Authorization_Policy_State (OD-D-B-05).
/// </summary>
[ApiController]
[Authorize]
[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]
[Route("api/v2/security/roles")]
public sealed class RolesController : ControllerBase
{
    private const string RequiredPermission = PermissionCodes.SecurityAdminManage;

    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ISecurityAdminService _service;

    public RolesController(IPermissionEvaluator permissionEvaluator, ISecurityAdminService service)
    {
        _permissionEvaluator = permissionEvaluator;
        _service = service;
    }

    /// <summary>GET /api/v2/security/roles</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        return Ok(await _service.ListRolesAsync(ct));
    }

    /// <summary>GET /api/v2/security/roles/{id}</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        return Ok(await _service.GetRoleAsync(id, ct));
    }

    /// <summary>POST /api/v2/security/roles</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        var role = await _service.CreateRoleAsync(actor, request, ct);
        return CreatedAtAction(nameof(Get), new { id = role.Id }, role);
    }

    /// <summary>PUT /api/v2/security/roles/{id}</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        var role = await _service.UpdateRoleAsync(actor, id, request, ct);
        return Ok(role);
    }

    /// <summary>
    /// DELETE /api/v2/security/roles/{id}
    /// Deactivates the role (soft delete, OD-D-B-07).
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(long id, [FromBody] DeactivateRoleRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        await _service.DeactivateRoleAsync(actor, id, request, ct);
        return NoContent();
    }

    /// <summary>POST /api/v2/security/roles/{id}/permissions</summary>
    [HttpPost("{id:long}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddPermissions(long id, [FromBody] AddRolePermissionsRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        await _service.AddRolePermissionsAsync(actor, id, request, ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v2/security/roles/{id}/permissions/{code}</summary>
    [HttpDelete("{id:long}/permissions/{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermission(long id, string code, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        await _service.RemoveRolePermissionAsync(actor, id, code, ct);
        return NoContent();
    }
}
