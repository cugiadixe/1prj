using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Api.Controllers.Security;

/// <summary>
/// GET /api/v2/security/permissions
/// GET /api/v2/security/permissions/{code}
///
/// Read-only access to the permission catalog.
/// Requires SECURITY_ADMIN_MANAGE (OD-D-B-02, OD-D-B-03).
/// </summary>
[ApiController]
[Authorize]
[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]
[Route("api/v2/security/permissions")]
public sealed class PermissionsController : ControllerBase
{
    private const string RequiredPermission = PermissionCodes.SecurityAdminManage;

    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ISecurityAdminService _service;

    public PermissionsController(IPermissionEvaluator permissionEvaluator, ISecurityAdminService service)
    {
        _permissionEvaluator = permissionEvaluator;
        _service = service;
    }

    /// <summary>GET /api/v2/security/permissions</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        var result = await _service.ListPermissionsAsync(ct);
        return Ok(result);
    }

    /// <summary>GET /api/v2/security/permissions/{code}</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string code, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        var result = await _service.GetPermissionAsync(code, ct);
        return Ok(result);
    }
}
