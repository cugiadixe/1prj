using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Api.Controllers.Security;

/// <summary>
/// Retrieves the final resolved set of effective permissions for a user (OD-D-B-10).
/// Only returns codes, no source breakdown (OD-D-B-11).
/// </summary>
[ApiController]
[Authorize]
[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]
[Route("api/v2/security/users/{userId:long}/effective-permissions")]
public sealed class EffectivePermissionsController : ControllerBase
{
    private const string RequiredPermission = PermissionCodes.SecurityAdminManage;

    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ISecurityAdminService _service;

    public EffectivePermissionsController(IPermissionEvaluator permissionEvaluator, ISecurityAdminService service)
    {
        _permissionEvaluator = permissionEvaluator;
        _service = service;
    }

    /// <summary>
    /// GET /api/v2/security/users/{userId}/effective-permissions?companyId={companyId}
    /// Caller must have SECURITY_ADMIN_MANAGE.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(EffectivePermissionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(long userId, [FromQuery] long? companyId, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        // Self-query is not allowed (OD-D-B-11)

        var response = await _service.GetEffectivePermissionsAsync(userId, companyId, ct);
        return Ok(response);
    }
}
