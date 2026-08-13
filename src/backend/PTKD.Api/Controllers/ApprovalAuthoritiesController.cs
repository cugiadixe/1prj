using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.ApprovalAuthorities.DTOs;
using PTKD.Application.ApprovalAuthorities.Services;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/approval-authorities")]
[Authorize]
[RequirePermission(PermissionCodes.ApprovalAuthorityManage, PermissionScope.Global)]
public class ApprovalAuthoritiesController : ControllerBase
{
    private readonly IApprovalAuthorityService _service;

    public ApprovalAuthoritiesController(IApprovalAuthorityService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] long? companyId,
        [FromQuery] long? departmentId,
        [FromQuery] bool includeClosed,
        CancellationToken ct)
    {
        return Ok(await _service.ListAsync(companyId, departmentId, includeClosed, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApprovalAuthorityRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(request, GetActorUserId(), ct);
        return Ok(dto);
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(long id, [FromBody] CloseApprovalAuthorityRequest request, CancellationToken ct)
    {
        var dto = await _service.CloseAsync(id, request, GetActorUserId(), ct);
        return Ok(dto);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
