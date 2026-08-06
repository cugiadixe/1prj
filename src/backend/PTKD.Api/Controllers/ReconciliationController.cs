using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.PaymentManagement.DTOs;
using PTKD.Application.PaymentManagement.Services;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/reconciliation")]
[Authorize]
public class ReconciliationController : ControllerBase
{
    private readonly IReconciliationService _reconciliationService;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public ReconciliationController(
        IReconciliationService reconciliationService,
        IPermissionEvaluator permissionEvaluator)
    {
        _reconciliationService = reconciliationService;
        _permissionEvaluator = permissionEvaluator;
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyReport(
        [FromQuery] long companyId,
        [FromQuery] DateTime date,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "RECONCILIATION_PREPARE", companyId, ct))
            return Forbid();

        var result = await _reconciliationService.GetDailyReportAsync(companyId, date, ct);
        return Ok(result);
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyReport(
        [FromQuery] long companyId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "RECONCILIATION_PREPARE", companyId, ct))
            return Forbid();

        var result = await _reconciliationService.GetMonthlyReportAsync(companyId, year, month, ct);
        return Ok(result);
    }

    [HttpPost("periods/{id:long}/prepare")]
    public async Task<IActionResult> Prepare(long id, [FromBody] PrepareReconciliationRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();

        var period = await _reconciliationService.GetPeriodByIdAsync(id, ct);
        if (period == null)
            return NotFound(new { Title = "Not Found", Detail = "Reconciliation period not found." });

        if (!await _permissionEvaluator.EvaluateAsync(userId, "RECONCILIATION_PREPARE", period.CompanyId, ct))
            return Forbid();

        try
        {
            var result = await _reconciliationService.PrepareAsync(id, request, userId, ct);
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

    [HttpPost("periods/{id:long}/confirm")]
    public async Task<IActionResult> Confirm(long id, [FromBody] ConfirmReconciliationRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();

        var period = await _reconciliationService.GetPeriodByIdAsync(id, ct);
        if (period == null)
            return NotFound(new { Title = "Not Found", Detail = "Reconciliation period not found." });

        if (!await _permissionEvaluator.EvaluateAsync(userId, "RECONCILIATION_CONFIRM", period.CompanyId, ct))
            return Forbid();

        try
        {
            var result = await _reconciliationService.ConfirmAsync(id, request, userId, ct);
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
