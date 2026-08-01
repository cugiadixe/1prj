using System;
using System.ComponentModel.DataAnnotations;
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

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/customers")]
[Authorize]
public class CustomerMasterChangeController : ControllerBase
{
    private readonly ICustomerMasterChangeService _service;

    public CustomerMasterChangeController(ICustomerMasterChangeService service)
    {
        _service = service;
    }

    [HttpPost("{id}/change-requests")]
    [RequirePermission(PermissionCodes.CustomerChangeRequestCreate, PermissionScope.Global)]
    public async Task<IActionResult> CreateChangeRequest([FromRoute] long id, [FromBody] CreateCustomerMasterChangeRequest request, CancellationToken ct)
    {
        var actorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actorIdStr) || !long.TryParse(actorIdStr, out var actorId))
            return Unauthorized();

        if (id != request.TargetCustomerId)
            return BadRequest(new { Message = "Route ID and request TargetCustomerId must match." });

        var companyIdStr = User.FindFirstValue("company_id");
        long? companyId = string.IsNullOrEmpty(companyIdStr) ? null : long.Parse(companyIdStr);

        var proposal = await _service.CreateChangeRequestAsync(request, actorId, companyId, ct);
        return Ok(proposal);
    }

    [HttpGet("my-change-requests")]
    [RequirePermission(PermissionCodes.CustomerChangeRequestCreate, PermissionScope.Global)]
    public async Task<IActionResult> GetMyChangeRequests(CancellationToken ct)
    {
        var actorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actorIdStr) || !long.TryParse(actorIdStr, out var actorId))
            return Unauthorized();

        var proposals = await _service.GetMyChangeRequestsAsync(actorId, ct);
        return Ok(proposals);
    }

    [HttpGet("change-requests/{requestId}")]
    [RequirePermission(PermissionCodes.CustomerChangeRequestCreate, PermissionScope.Global)]
    public async Task<IActionResult> GetChangeRequestById([FromRoute] long requestId, CancellationToken ct)
    {
        var proposal = await _service.GetChangeRequestByIdAsync(requestId, ct);
        if (proposal == null) return NotFound();

        // Enforce basic scope: must be requester OR have global review permission.
        // For simplicity, we just allow the requester. Reviewers would use workflow endpoints or admin endpoints.
        var actorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(actorIdStr) && long.TryParse(actorIdStr, out var actorId))
        {
            if (proposal.RequesterId != actorId)
            {
                // In a real app we check for AdminView permission here.
                var hasAdminView = User.HasClaim(c => c.Type == "permission" && c.Value == "CUSTOMER_CHANGE_REQUEST_ADMIN_VIEW");
                if (!hasAdminView)
                    return Forbid();
            }
        }

        return Ok(proposal);
    }
}
