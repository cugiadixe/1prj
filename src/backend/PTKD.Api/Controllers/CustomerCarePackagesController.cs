using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.CustomerCarePackages.DTOs;
using PTKD.Application.CustomerCarePackages.Services;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/customer-care-packages")]
[Authorize]
public class CustomerCarePackagesController : ControllerBase
{
    private readonly ICustomerCarePackageService _service;

    public CustomerCarePackagesController(ICustomerCarePackageService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CustomerCarePackageView, PermissionScope.Global)]
    public async Task<IActionResult> List([FromQuery] long? customerId, [FromQuery] long? graveId, CancellationToken ct)
    {
        if (customerId.HasValue)
            return Ok(await _service.ListByCustomerAsync(customerId.Value, ct));
        if (graveId.HasValue)
            return Ok(await _service.ListByGraveAsync(graveId.Value, ct));
        return BadRequest(new { error = "customerId hoặc graveId là bắt buộc." });
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.CustomerCarePackageManage, PermissionScope.Global)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCarePackageRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(request, GetActorUserId(), ct);
        return Ok(dto);
    }

    [HttpPost("{id}/assign-grave")]
    [RequirePermission(PermissionCodes.CustomerCarePackageManage, PermissionScope.Global)]
    public async Task<IActionResult> AssignGrave(long id, [FromBody] AssignGraveRequest request, CancellationToken ct)
    {
        var dto = await _service.AssignGraveAsync(id, request.GraveId, GetActorUserId(), ct);
        return Ok(dto);
    }

    [HttpPost("{id}/cancel")]
    [RequirePermission(PermissionCodes.CustomerCarePackageManage, PermissionScope.Global)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var dto = await _service.CancelAsync(id, GetActorUserId(), ct);
        return Ok(dto);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
