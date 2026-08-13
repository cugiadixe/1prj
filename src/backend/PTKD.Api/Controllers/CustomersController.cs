using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Services;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public CustomersController(ICustomerService customerService, IPermissionEvaluator permissionEvaluator)
    {
        _customerService = customerService;
        _permissionEvaluator = permissionEvaluator;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.Global)]
    public async Task<IActionResult> Search([FromQuery] CustomerSearchRequest request, CancellationToken ct)
    {
        var canViewSensitive = await HasPermissionAsync(PermissionCodes.CustomerViewSensitive, ct);
        var result = await _customerService.SearchCustomersAsync(request, canViewSensitive, ct);
        return Ok(result);
    }

    [HttpGet("lookups/companies")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.Global)]
    public async Task<IActionResult> GetCompanyLookups(CancellationToken ct)
    {
        var companies = await _customerService.GetAssignedCompanyLookupsAsync(ct);
        return Ok(companies);
    }

    [HttpGet("lookups/staff")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.Global)]
    public async Task<IActionResult> GetStaffLookups(CancellationToken ct)
    {
        var staff = await _customerService.GetAssignedStaffLookupsAsync(ct);
        return Ok(staff);
    }

    [HttpGet("{id}")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.Global)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var canViewSensitive = await HasPermissionAsync(PermissionCodes.CustomerViewSensitive, ct);
        var customer = await _customerService.GetCustomerByIdAsync(id, canViewSensitive, ct);
        if (customer == null) return NotFound();
        return Ok(customer);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.CustomerCreateFinal, PermissionScope.Global)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var customer = await _customerService.CreateCustomerAsync(request, actorUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    [RequirePermission(PermissionCodes.CustomerMasterUpdate, PermissionScope.Global)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var customer = await _customerService.UpdateCustomerAsync(id, request, actorUserId, ct);
        return Ok(customer);
    }

    [HttpGet("{id}/company-contexts")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.Global)]
    public async Task<IActionResult> GetCompanyContexts(long id, CancellationToken ct)
    {
        var contexts = await _customerService.GetCompanyContextsAsync(id, ct);
        return Ok(contexts);
    }

    [HttpPost("{id}/company-contexts")]
    [RequirePermission(PermissionCodes.CustomerCreateFinal, PermissionScope.Global)]
    public async Task<IActionResult> CreateCompanyContext(long id, [FromBody] CreateCustomerCompanyContextRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var ctx = await _customerService.CreateCompanyContextAsync(id, request, actorUserId, ct);
        return CreatedAtAction(nameof(GetCompanyContexts), new { id }, ctx);
    }

    [HttpPut("{id}/company-contexts/{contextId}")]
    [RequirePermission(PermissionCodes.CustomerMasterUpdate, PermissionScope.Global)]
    public async Task<IActionResult> UpdateCompanyContext(long id, long contextId, [FromBody] UpdateCustomerCompanyContextRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var ctx = await _customerService.UpdateCompanyContextAsync(id, contextId, request, actorUserId, ct);
        return Ok(ctx);
    }

    [HttpGet("duplicate-check")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.Global)]
    public async Task<IActionResult> DuplicateCheck([FromQuery] DuplicateCheckRequest request, CancellationToken ct)
    {
        var result = await _customerService.CheckDuplicatesAsync(request, ct);
        return Ok(result);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }

    private async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken ct)
    {
        var userId = GetActorUserId();
        return await _permissionEvaluator.EvaluateAsync(userId, permissionCode, null, ct);
    }
}
