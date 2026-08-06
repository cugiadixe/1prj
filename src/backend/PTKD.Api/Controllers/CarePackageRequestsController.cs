using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.CarePackages.DTOs;
using PTKD.Application.CarePackages.Services;
using PTKD.Application.Common.Models;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/care-packages")]
[Authorize]
public class CarePackageRequestsController : ControllerBase
{
    private readonly ICarePackageRequestService _service;

    public CarePackageRequestsController(ICarePackageRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CarePackageView, PermissionScope.Company)]
    public async Task<ActionResult<PagedResult<CarePackageRequestDto>>> List(
        [FromHeader(Name = "X-Company-Id")] long companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(companyId, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [RequirePermission(PermissionCodes.CarePackageView, PermissionScope.Company)]
    public async Task<ActionResult<CarePackageRequestDto>> GetById(long id, [FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var dto = await _service.GetByIdAsync(companyId, id, ct);

        if (dto == null)
            return NotFound();

        return Ok(dto);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.CarePackageCreate, PermissionScope.Company)]
    public async Task<ActionResult<CarePackageRequestDto>> Create(
        [FromHeader(Name = "X-Company-Id")] long companyId,
        [FromBody] CreateCarePackageRequest request,
        CancellationToken ct)
    {
        var userId = GetActorUserId();
        var dto = await _service.CreateAsync(companyId, request, userId, ct);

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }

    [HttpPost("{id}/submit")]
    [RequirePermission(PermissionCodes.CarePackageCreate, PermissionScope.Company)]
    public async Task<IActionResult> Submit(long id, [FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var result = await _service.SubmitAsync(id, companyId, GetActorUserId(), ct);
        return Ok(result);
    }

    public class ApproveRejectRequest
    {
        public long StepId { get; set; }
        public string TargetVersion { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string Comment { get; set; } = "";
    }

    [HttpPost("{id}/approve")]
    [RequirePermission(PermissionCodes.CarePackageApprove, PermissionScope.Company)]
    public async Task<IActionResult> Approve(long id, [FromHeader(Name = "X-Company-Id")] long companyId, [FromBody] ApproveRejectRequest request, CancellationToken ct)
    {
        var result = await _service.ApproveStepAsync(id, request.StepId, request.TargetVersion, request.Reason, request.Comment, companyId, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpPost("{id}/reject")]
    [RequirePermission(PermissionCodes.CarePackageReject, PermissionScope.Company)]
    public async Task<IActionResult> Reject(long id, [FromHeader(Name = "X-Company-Id")] long companyId, [FromBody] ApproveRejectRequest request, CancellationToken ct)
    {
        var result = await _service.RejectStepAsync(id, request.StepId, request.TargetVersion, request.Reason, request.Comment, companyId, GetActorUserId(), ct);
        return Ok(result);
    }

    public class CreatePaymentRequest
    {
        public string PaymentMethod { get; set; } = null!;
    }

    [HttpPost("{id}/create-payment")]
    [RequirePermission(PermissionCodes.CarePackageCreatePayment, PermissionScope.Company)]
    public async Task<IActionResult> CreatePayment(long id, [FromHeader(Name = "X-Company-Id")] long companyId, [FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var result = await _service.CreatePaymentAsync(id, request.PaymentMethod, companyId, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpGet("{id}/payment-status")]
    [RequirePermission(PermissionCodes.CarePackageView, PermissionScope.Company)]
    public async Task<IActionResult> GetPaymentStatus(long id, [FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var result = await _service.GetPaymentStatusAsync(id, companyId, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("{id}/activate")]
    [RequirePermission(PermissionCodes.CarePackageCreate, PermissionScope.Company)]
    public async Task<IActionResult> Activate(long id, [FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var result = await _service.ActivateAsync(id, companyId, GetActorUserId(), ct);
        return Ok(result);
    }
}
