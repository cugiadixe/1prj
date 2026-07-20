using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Api.Controllers.Security;

/// <summary>
/// CRUD for AdminGroups and their permission assignments.
/// All mutations require SECURITY_ADMIN_MANAGE (OD-D-B-02, OD-D-B-03).
/// Deactivation is soft (OD-D-B-07). Mutations increment Authorization_Policy_State (OD-D-B-05).
/// </summary>
[ApiController]
[Authorize]
[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]
[Route("api/v2/security/admin-groups")]
public sealed class AdminGroupsController : ControllerBase
{
    private const string RequiredPermission = PermissionCodes.SecurityAdminManage;

    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ISecurityAdminService _service;

    public AdminGroupsController(IPermissionEvaluator permissionEvaluator, ISecurityAdminService service)
    {
        _permissionEvaluator = permissionEvaluator;
        _service = service;
    }

    /// <summary>GET /api/v2/security/admin-groups</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminGroupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        return Ok(await _service.ListAdminGroupsAsync(ct));
    }

    /// <summary>GET /api/v2/security/admin-groups/{id}</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AdminGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        return Ok(await _service.GetAdminGroupAsync(id, ct));
    }

    /// <summary>POST /api/v2/security/admin-groups</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateAdminGroupRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        var group = await _service.CreateAdminGroupAsync(actor, request, ct);
        return CreatedAtAction(nameof(Get), new { id = group.Id }, group);
    }

    /// <summary>PUT /api/v2/security/admin-groups/{id}</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(AdminGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateAdminGroupRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        var group = await _service.UpdateAdminGroupAsync(actor, id, request, ct);
        return Ok(group);
    }

    /// <summary>
    /// DELETE /api/v2/security/admin-groups/{id}
    /// Soft deactivate (OD-D-B-07).
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(long id, [FromBody] DeactivateAdminGroupRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        await _service.DeactivateAdminGroupAsync(actor, id, request, ct);
        return NoContent();
    }

    /// <summary>POST /api/v2/security/admin-groups/{id}/permissions</summary>
    [HttpPost("{id:long}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddPermissions(long id, [FromBody] AddAdminGroupPermissionsRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        await _service.AddAdminGroupPermissionsAsync(actor, id, request, ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v2/security/admin-groups/{id}/permissions/{code}</summary>
    [HttpDelete("{id:long}/permissions/{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemovePermission(long id, string code, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        await _service.RemoveAdminGroupPermissionAsync(actor, id, code, ct);
        return NoContent();
    }
}
