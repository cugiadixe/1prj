using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.CarePackages.DTOs;
using PTKD.Application.CarePackages.Services;
using PTKD.Application.Common.Models;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/care-packages")]
[Authorize]
public class CarePackageRequestsController : ControllerBase
{
    private readonly ICarePackageRequestService _service;

    public CarePackageRequestsController(ICarePackageRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CarePackageView, PermissionScope.Company)]
    public async Task<ActionResult<PagedResult<CarePackageRequestDto>>> List(
        [FromHeader(Name = "X-Company-Id")] long companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(companyId, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [RequirePermission(PermissionCodes.CarePackageView, PermissionScope.Company)]
    public async Task<ActionResult<CarePackageRequestDto>> GetById(long id, [FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(companyId, id, ct);

        if (dto == null)
            return NotFound();

        return Ok(dto);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.CarePackageCreate, PermissionScope.Company)]
    public async Task<ActionResult<CarePackageRequestDto>> Create(
        [FromHeader(Name = "X-Company-Id")] long companyId,
        [FromBody] CreateCarePackageRequest request,
        CancellationToken ct)
    {
        var userId = GetActorUserId();
        var dto = await _service.CreateAsync(companyId, request, userId, ct);

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
