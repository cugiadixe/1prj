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
[Route("api/v2/payments")]
[Authorize]
// Đường tự-kiểm bằng EvaluateAsync: quyền GLOBAL được phép thao tác xuyên công ty (KHÔNG đòi thành
// viên). QUYẾT ĐỊNH có chủ đích SR-06 (docs/decisions/2026-08-18-security-review-owner-decisions.md).
public class PaymentTransactionController : ControllerBase
{
    private readonly IPaymentTransactionService _paymentService;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public PaymentTransactionController(
        IPaymentTransactionService paymentService,
        IPermissionEvaluator permissionEvaluator)
    {
        _paymentService = paymentService;
        _permissionEvaluator = permissionEvaluator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDraft([FromBody] CreatePaymentDraftRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "PAYMENT_CREATE_DRAFT", request.CompanyId, ct))
            return Forbid();

        try
        {
            var result = await _paymentService.CreateDraftAsync(request, userId, ct);
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

    [HttpPost("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(long id, [FromBody] ConfirmPaymentRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var payment = await _paymentService.GetByIdAsync(id, ct);
        if (payment == null)
            return NotFound(new { Title = "Not Found", Detail = "Payment not found." });

        if (!await _permissionEvaluator.EvaluateAsync(userId, "PAYMENT_CONFIRM", payment.CompanyId, ct))
            return Forbid();

        try
        {
            var result = await _paymentService.ConfirmAsync(id, request, userId, ct);
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

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] long companyId,
        [FromQuery] long? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (!await _permissionEvaluator.EvaluateAsync(userId, "PAYMENT_CREATE_DRAFT", companyId, ct))
            return Forbid();

        var result = await _paymentService.ListAsync(companyId, customerId, status, dateFrom, dateTo, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var result = await _paymentService.GetByIdAsync(id, ct);
        if (result == null)
            return NotFound(new { Title = "Not Found", Detail = "Payment not found." });

        if (!await _permissionEvaluator.EvaluateAsync(userId, "PAYMENT_CREATE_DRAFT", result.CompanyId, ct))
            return Forbid();

        return Ok(result);
    }

    [HttpPost("{id:long}/correct")]
    public async Task<IActionResult> CorrectConfirmed(long id, [FromBody] CorrectPaymentRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var payment = await _paymentService.GetByIdAsync(id, ct);
        if (payment == null)
            return NotFound(new { Title = "Not Found", Detail = "Payment not found." });

        if (!await _permissionEvaluator.EvaluateAsync(userId, "PAYMENT_CORRECT_CONFIRMED", payment.CompanyId, ct))
            return Forbid();

        try
        {
            var result = await _paymentService.CorrectConfirmedAsync(id, request, userId, ct);
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

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> SoftDelete(long id, [FromBody] SoftDeletePaymentRequest request, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var payment = await _paymentService.GetByIdAsync(id, ct);
        if (payment == null)
            return NotFound(new { Title = "Not Found", Detail = "Payment not found." });

        // Dùng chung quyền PAYMENT_CREATE_DRAFT có chủ đích (SR-09): xoá mềm CHỈ tác động bản nháp —
        // PaymentTransaction.SoftDelete() gọi EnsureNotConfirmed() chặn payment đã xác nhận. "Ai tạo
        // nháp thì xoá nháp". Nếu sau này mở xoá cho payment đã xác nhận thì phải tách quyền riêng.
        if (!await _permissionEvaluator.EvaluateAsync(userId, "PAYMENT_CREATE_DRAFT", payment.CompanyId, ct))
            return Forbid();

        try
        {
            await _paymentService.SoftDeleteDraftAsync(id, request, userId, ct);
            return NoContent();
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
