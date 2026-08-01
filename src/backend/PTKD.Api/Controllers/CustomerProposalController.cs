using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Services;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/customer-proposals")]
[Authorize]
public class CustomerProposalController : ControllerBase
{
    private readonly ICustomerProposalService _proposalService;

    public CustomerProposalController(ICustomerProposalService proposalService)
    {
        _proposalService = proposalService;
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.CustomerChangeRequestCreate, PermissionScope.Global)]
    public async Task<IActionResult> CreateProposal([FromBody] CreateCustomerProposalRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var proposal = await _proposalService.CreateProposalAsync(request, actorUserId, ct);
        return CreatedAtAction(nameof(GetProposal), new { id = proposal.Id }, proposal);
    }

    [HttpGet("{id}")]
    [RequirePermission(PermissionCodes.CustomerChangeRequestCreate, PermissionScope.Global)]
    public async Task<IActionResult> GetProposal(long id, CancellationToken ct)
    {
        var proposal = await _proposalService.GetProposalByIdAsync(id, ct);
        if (proposal == null) return NotFound();
        return Ok(proposal);
    }

    [HttpGet("my-proposals")]
    public async Task<IActionResult> GetMyProposals(CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var proposals = await _proposalService.GetMyProposalsAsync(actorUserId, ct);
        return Ok(proposals);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
