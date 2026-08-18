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
[Route("api/v2/services")]
[Authorize]
// Đường tự-kiểm bằng EvaluateAsync: quyền GLOBAL được phép thao tác xuyên công ty (KHÔNG đòi thành
// viên). Đây là QUYẾT ĐỊNH có chủ đích SR-06 (docs/decisions/2026-08-18-security-review-owner-decisions.md),
// không phải lỗ hổng — đừng "vá" thêm chốt thành viên nếu chưa xem lại quyết định đó.
public class ServiceController : ControllerBase
{
    private readonly IServiceService _serviceService;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public ServiceController(
        IServiceService serviceService,
        IPermissionEvaluator permissionEvaluator)
    {
        _serviceService = serviceService;
        _permissionEvaluator = permissionEvaluator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] long? companyId = null,
        [FromQuery] long? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (companyId.HasValue)
        {
            if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_VIEW", companyId.Value, ct))
                return Forbid();
        }
        else
        {
            // Xem TẤT CẢ công ty: yêu cầu quyền SERVICE_VIEW ở phạm vi toàn cục (grant GLOBAL)
            var globalPerms = await _permissionEvaluator.GetEffectivePermissionsAsync(userId, null, ct);
            if (!globalPerms.Contains("SERVICE_VIEW"))
                return Forbid();
        }

        var result = await _serviceService.ListAsync(companyId, customerId, status, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
    {
        var userId = GetUserId();

        var result = await _serviceService.GetByIdAsync(id, ct);
        if (result == null)
            return NotFound(new { Title = "Not Found", Detail = "Service not found." });

        if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_VIEW", result.CompanyId, ct))
            return Forbid();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_CREATE_STANDARD", request.CompanyId, ct))
            return Forbid();

        try
        {
            var result = await _serviceService.CreateStandardAsync(request, userId, ct);
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

    [HttpPost("{id:long}/renew")]
    public async Task<IActionResult> Renew(long id, [FromBody] RenewServiceRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();

        var existing = await _serviceService.GetByIdAsync(id, ct);
        if (existing == null)
            return NotFound(new { Title = "Not Found", Detail = "Service not found." });

        if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_RENEW_STANDARD", existing.CompanyId, ct))
            return Forbid();

        try
        {
            var result = await _serviceService.RenewStandardAsync(id, request, userId, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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

    [HttpPost("{id:long}/request-price-override")]
    public async Task<IActionResult> RequestPriceOverride(long id, [FromBody] RequestPriceOverrideRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();

        var existing = await _serviceService.GetByIdAsync(id, ct);
        if (existing == null)
            return NotFound(new { Title = "Not Found", Detail = "Service not found." });

        if (!await _permissionEvaluator.EvaluateAsync(userId, "SERVICE_PRICE_OVERRIDE_REQUEST", existing.CompanyId, ct))
            return Forbid();

        try
        {
            var workflowInstanceId = await _serviceService.RequestPriceOverrideAsync(id, request, userId, ct);
            return Ok(new { WorkflowInstanceId = workflowInstanceId });
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
