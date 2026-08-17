using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Cemeteries;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/cemeteries")]
[Authorize]
public class CemeteriesController : ControllerBase
{
    private readonly ICemeteryService _service;

    public CemeteriesController(ICemeteryService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CardIssue, PermissionScope.Company)]
    public async Task<IActionResult> GetAll([FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var result = await _service.GetByCompanyAsync(companyId, ct);
        return Ok(result);
    }

    [HttpPut("{id}/watermark")]
    [RequirePermission(PermissionCodes.CardIssue, PermissionScope.Company)]
    public async Task<IActionResult> SetWatermark(long id, [FromHeader(Name = "X-Company-Id")] long companyId, [FromBody] SetWatermarkRequest request, CancellationToken ct)
    {
        await _service.SetWatermarkAsync(id, request.WatermarkCode, companyId, GetActorUserId(), ct);
        return NoContent();
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
