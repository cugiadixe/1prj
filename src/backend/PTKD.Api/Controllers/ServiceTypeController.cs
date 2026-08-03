using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.ServiceManagement.DTOs;
using PTKD.Application.ServiceManagement.Services;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/service-types")]
[Authorize]
public class ServiceTypeController : ControllerBase
{
    private readonly IServiceTypeService _serviceTypeService;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public ServiceTypeController(
        IServiceTypeService serviceTypeService,
        IPermissionEvaluator permissionEvaluator)
    {
        _serviceTypeService = serviceTypeService;
        _permissionEvaluator = permissionEvaluator;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_TYPE_MANAGE", null, ct))
            return Forbid();

        var result = await _serviceTypeService.ListAsync(page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_TYPE_MANAGE", null, ct))
            return Forbid();

        var result = await _serviceTypeService.GetByIdAsync(id, ct);
        if (result == null)
            return NotFound(new { Title = "Not Found", Detail = "Service type not found." });

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceTypeRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_TYPE_MANAGE", null, ct))
            return Forbid();

        try
        {
            var result = await _serviceTypeService.CreateAsync(request, userId, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Title = "Validation Error", Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Title = "Validation Error", Detail = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateServiceTypeRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_TYPE_MANAGE", null, ct))
            return Forbid();

        try
        {
            var result = await _serviceTypeService.UpdateAsync(id, request, userId, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Title = "Validation Error", Detail = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Title = "Conflict", Detail = "Data has changed since you started. Please refresh and try again." });
        }
    }

    [HttpPost("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, [FromBody] DeactivateRequest? request = null, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_TYPE_MANAGE", null, ct))
            return Forbid();

        try
        {
            var result = await _serviceTypeService.DeactivateAsync(id, request?.RowVersion ?? "", userId, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Title = "Validation Error", Detail = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Title = "Conflict", Detail = "Data has changed since you started. Please refresh and try again." });
        }
    }

    private long GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.Parse(claim!);
    }
}

public class DeactivateRequest
{
    public string RowVersion { get; set; } = null!;
}
