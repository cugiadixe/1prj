using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Cards.DTOs;
using PTKD.Application.Cards.Services;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/cards")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly ICardService _service;

    public CardsController(ICardService service)
    {
        _service = service;
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.CardIssue, PermissionScope.Company)]
    public async Task<IActionResult> Create([FromHeader(Name = "X-Company-Id")] long companyId, [FromBody] CreateCardRequest request, CancellationToken ct)
    {
        var result = await _service.CreateCardFromGraveAsync(request.GraveId, companyId, request.ServiceId, GetActorUserId(), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CardIssue, PermissionScope.Company)]
    public async Task<IActionResult> GetAll([FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var result = await _service.GetByCompanyAsync(companyId, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [RequirePermission(PermissionCodes.CardIssue, PermissionScope.Company)]
    public async Task<IActionResult> GetById(long id, [FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, companyId, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
