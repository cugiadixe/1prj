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
/// Manages baseline permissions granted at the department level.
/// Required per OD-D-B-09 because the evaluator uses Department_Permissions.
/// Mutations require SECURITY_ADMIN_MANAGE and increment Authorization_Policy_State (OD-D-B-05).
/// </summary>
[ApiController]
[Authorize]
[RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]
[Route("api/v2/security/departments/{departmentId:long}/permissions")]
public sealed class DepartmentPermissionsController : ControllerBase
{
    private const string RequiredPermission = PermissionCodes.SecurityAdminManage;

    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ISecurityAdminService _service;

    public DepartmentPermissionsController(IPermissionEvaluator permissionEvaluator, ISecurityAdminService service)
    {
        _permissionEvaluator = permissionEvaluator;
        _service = service;
    }

    /// <summary>GET /api/v2/security/departments/{departmentId}/permissions</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentPermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(long departmentId, CancellationToken ct)
    {
        return Ok(await _service.ListDepartmentPermissionsAsync(departmentId, ct));
    }

    /// <summary>
    /// PUT /api/v2/security/departments/{departmentId}/permissions
    /// Replaces the entire baseline permission set for the department.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetPermissions(long departmentId, [FromBody] SetDepartmentPermissionsRequest request, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        await _service.SetDepartmentPermissionsAsync(actor, departmentId, request, ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v2/security/departments/{departmentId}/permissions/{code}</summary>
    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemovePermission(long departmentId, string code, CancellationToken ct)
    {
        var actor = SecurityControllerHelper.GetActorUserId(User);

        await _service.RemoveDepartmentPermissionAsync(actor, departmentId, code, ct);
        return NoContent();
    }
}
