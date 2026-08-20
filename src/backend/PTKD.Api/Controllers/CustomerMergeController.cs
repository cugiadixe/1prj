using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Services;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Api.Controllers;

[ApiController]
[Authorize]
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
        // Chỉ cần CÓ quyền ở BẤT KỲ công ty nào (không đòi toàn cục): CheckDuplicatesAsync tự lọc kết
        // quả theo phạm vi công ty người gọi. Dùng EvaluateAsync(..., null) sẽ đòi cấp toàn cục và chặn
        // nhầm người chỉ có quyền theo công ty.
        var scope = await _permissionEvaluator.ResolveAsync(userId, "CUSTOMER_MERGE_REQUEST_CREATE", ct);
        if (!scope.Granted)
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
        // Chỉ cần CÓ quyền ở BẤT KỲ công ty nào — CustomerMergeService tự kiểm người tạo có được thao
        // tác trên khách nguồn/đích theo phạm vi công ty (ném 403 nếu ngoài phạm vi).
        var scope = await _permissionEvaluator.ResolveAsync(userId, "CUSTOMER_MERGE_REQUEST_CREATE", ct);
        if (!scope.Granted)
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

    [HttpPost("merge-requests/{id}/submit")]
    public async Task<IActionResult> SubmitMergeRequest(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        // Chỉ cần CÓ quyền tạo/gộp ở BẤT KỲ công ty nào — service tự kiểm phạm vi trên khách nguồn/đích.
        var scope = await _permissionEvaluator.ResolveAsync(userId, "CUSTOMER_MERGE_REQUEST_CREATE", ct);
        if (!scope.Granted)
        {
            return Forbid();
        }

        var companyIdStr = User.FindFirst("company_id")?.Value;
        long? companyId = string.IsNullOrEmpty(companyIdStr) ? null : long.Parse(companyIdStr);

        try
        {
            var result = await _mergeService.SubmitMergeRequestAsync(id, userId, companyId, ct);
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
        // Có quyền view HOẶC admin-view ở bất kỳ công ty nào là qua cổng; service lọc theo phạm vi.
        var canView = (await _permissionEvaluator.ResolveAsync(userId, "CUSTOMER_MERGE_REQUEST_VIEW", ct)).Granted;
        var canViewAdmin = (await _permissionEvaluator.ResolveAsync(userId, "CUSTOMER_MERGE_REQUEST_ADMIN_VIEW", ct)).Granted;

        if (!canView && !canViewAdmin)
        {
            return Forbid();
        }

        var result = await _mergeService.GetMergeRequestByIdAsync(id, userId, ct);
        if (result == null) return NotFound();

        return Ok(result);
    }

    [HttpGet("merge-requests")]
    public async Task<IActionResult> ListMergeRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        // Có quyền view HOẶC admin-view ở bất kỳ công ty nào là qua cổng; service lọc theo phạm vi.
        var canView = (await _permissionEvaluator.ResolveAsync(userId, "CUSTOMER_MERGE_REQUEST_VIEW", ct)).Granted;
        var canViewAdmin = (await _permissionEvaluator.ResolveAsync(userId, "CUSTOMER_MERGE_REQUEST_ADMIN_VIEW", ct)).Granted;

        if (!canView && !canViewAdmin)
        {
            return Forbid();
        }

        var result = await _mergeService.SearchMergeRequestsAsync(page, pageSize, userId, ct);
        return Ok(result);
    }
}
