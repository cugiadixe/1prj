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
[Route("api/v2/card-reprint-requests")]
[Authorize]
public class CardReprintRequestsController : ControllerBase
{
    private readonly ICardReprintRequestService _service;

    public CardReprintRequestsController(ICardReprintRequestService service)
    {
        _service = service;
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.CardReprintRequestCreate, PermissionScope.Company)]
    public async Task<IActionResult> Create([FromHeader(Name = "X-Company-Id")] long companyId, [FromBody] CreateCardReprintRequest request, CancellationToken ct)
    {
        if (request.CompanyId != companyId)
        {
            return BadRequest(new { Error = "Company ID in request body must match X-Company-Id header." });
        }
        var actorUserId = GetActorUserId();
        var result = await _service.CreateRequestAsync(request, actorUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CardReprintRequestView, PermissionScope.Company)]
    public async Task<IActionResult> GetAll([FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var result = await _service.GetRequestsAsync(companyId, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [RequirePermission(PermissionCodes.CardReprintRequestView, PermissionScope.Company)]
    public async Task<IActionResult> GetById(long id, [FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var result = await _service.GetRequestByIdAsync(id, companyId, ct);
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
