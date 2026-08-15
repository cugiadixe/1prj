using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Services;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/customers")]
public class CustomerMergeController : ControllerBase
{
    private readonly ICustomerMergeService _mergeService;
    private readonly ICustomerService _customerService;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public CustomerMergeController(
        ICustomerMergeService mergeService,
        ICustomerService customerService,
        IPermissionEvaluator permissionEvaluator)
    {
        _mergeService = mergeService;
        _customerService = customerService;
        _permissionEvaluator = permissionEvaluator;
    }

    private long GetUserId()
    {
        var subjectClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(subjectClaim, out var userId))
            return userId;
        throw new UnauthorizedAccessException();
    }

    [HttpGet("duplicates")]
    public async Task<IActionResult> FindDuplicates([FromQuery] string? cccd, [FromQuery] string? phone, CancellationToken ct)
    {
        var userId = GetUserId();
        var hasPermission = await _permissionEvaluator.EvaluateAsync(userId, "CUSTOMER_MERGE_REQUEST_CREATE", null, ct);
        if (!hasPermission)
        {
            return Forbid();
        }

        var request = new DuplicateCheckRequest { Cccd = cccd, Phone = phone };
        var result = await _customerService.CheckDuplicatesAsync(request, userId, ct);
        return Ok(result);
    }

    [HttpPost("merge-requests")]
    public async Task<IActionResult> CreateMergeRequest([FromBody] CreateCustomerMergeRequestDto request, CancellationToken ct)
    {
        var userId = GetUserId();
        var hasPermission = await _permissionEvaluator.EvaluateAsync(userId, "CUSTOMER_MERGE_REQUEST_CREATE", null, ct);
        if (!hasPermission)
        {
            return Forbid();
        }

        try
        {
            var result = await _mergeService.CreateMergeRequestAsync(request, userId, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Title = "Validation Error", Detail = ex.Message });
        }
    }

    [HttpGet("merge-requests/{id}")]
    public async Task<IActionResult> GetMergeRequest(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var canView = await _permissionEvaluator.EvaluateAsync(userId, "CUSTOMER_MERGE_REQUEST_VIEW", null, ct);
        var canViewAdmin = await _permissionEvaluator.EvaluateAsync(userId, "CUSTOMER_MERGE_REQUEST_ADMIN_VIEW", null, ct);

        if (!canView && !canViewAdmin)
        {
            return Forbid();
        }

        var result = await _mergeService.GetMergeRequestByIdAsync(id, ct);
        if (result == null) return NotFound();

        return Ok(result);
    }

    [HttpGet("merge-requests")]
    public async Task<IActionResult> ListMergeRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var canView = await _permissionEvaluator.EvaluateAsync(userId, "CUSTOMER_MERGE_REQUEST_VIEW", null, ct);
        var canViewAdmin = await _permissionEvaluator.EvaluateAsync(userId, "CUSTOMER_MERGE_REQUEST_ADMIN_VIEW", null, ct);

        if (!canView && !canViewAdmin)
        {
            return Forbid();
        }

        var result = await _mergeService.SearchMergeRequestsAsync(page, pageSize, ct);
        return Ok(result);
    }
}
