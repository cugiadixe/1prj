using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Customers.Services;
using PTKD.Application.Relationships.DTOs;
using PTKD.Application.Relationships.Services;
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
    private readonly ICustomerRelationshipService _relationshipService;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public CustomersController(
        ICustomerService customerService,
        ICustomerRelationshipService relationshipService,
        IPermissionEvaluator permissionEvaluator)
    {
        _customerService = customerService;
        _relationshipService = relationshipService;
        _permissionEvaluator = permissionEvaluator;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> Search([FromQuery] CustomerSearchRequest request, CancellationToken ct)
    {
        var canViewSensitive = await HasPermissionAsync(PermissionCodes.CustomerViewSensitive, ct);
        var result = await _customerService.SearchCustomersAsync(request, canViewSensitive, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpGet("lookups/companies")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> GetCompanyLookups(CancellationToken ct)
    {
        var companies = await _customerService.GetAssignedCompanyLookupsAsync(GetActorUserId(), ct);
        return Ok(companies);
    }

    [HttpGet("lookups/staff")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> GetStaffLookups(CancellationToken ct)
    {
        var staff = await _customerService.GetAssignedStaffLookupsAsync(GetActorUserId(), ct);
        return Ok(staff);
    }

    [HttpGet("{id}")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var canViewSensitive = await HasPermissionAsync(PermissionCodes.CustomerViewSensitive, ct);
        var customer = await _customerService.GetCustomerByIdAsync(id, canViewSensitive, GetActorUserId(), ct);
        if (customer == null) return NotFound();
        return Ok(customer);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.CustomerCreateFinal, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var customer = await _customerService.CreateCustomerAsync(request, actorUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    [RequirePermission(PermissionCodes.CustomerMasterUpdate, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var customer = await _customerService.UpdateCustomerAsync(id, request, actorUserId, ct);
        return Ok(customer);
    }

    // Bảng điều khiển 360: mộ khách SỞ HỮU + mộ khách ĐƯỢC AN TÁNG. Dữ liệu mộ lọc theo GRAVE_VIEW
    // riêng bên trong service; người không có quyền mộ nhận rỗng + cờ GraveAccessDenied.
    [HttpGet("{id}/overview")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> GetOverview(long id, CancellationToken ct)
    {
        var overview = await _customerService.GetCustomerOverviewAsync(id, GetActorUserId(), ct);
        if (overview == null) return NotFound();
        return Ok(overview);
    }

    [HttpGet("{id}/company-contexts")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> GetCompanyContexts(long id, CancellationToken ct)
    {
        var contexts = await _customerService.GetCompanyContextsAsync(id, GetActorUserId(), ct);
        return Ok(contexts);
    }

    [HttpPost("{id}/company-contexts")]
    [RequirePermission(PermissionCodes.CustomerCreateFinal, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> CreateCompanyContext(long id, [FromBody] CreateCustomerCompanyContextRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var ctx = await _customerService.CreateCompanyContextAsync(id, request, actorUserId, ct);
        return CreatedAtAction(nameof(GetCompanyContexts), new { id }, ctx);
    }

    [HttpPut("{id}/company-contexts/{contextId}")]
    [RequirePermission(PermissionCodes.CustomerMasterUpdate, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> UpdateCompanyContext(long id, long contextId, [FromBody] UpdateCustomerCompanyContextRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var ctx = await _customerService.UpdateCompanyContextAsync(id, contextId, request, actorUserId, ct);
        return Ok(ctx);
    }

    [HttpGet("duplicate-check")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> DuplicateCheck([FromQuery] DuplicateCheckRequest request, CancellationToken ct)
    {
        var result = await _customerService.CheckDuplicatesAsync(request, GetActorUserId(), ct);
        return Ok(result);
    }

    // ─── Quan hệ gia đình (đồ thị Customer_Relationships) ────────────────────────

    [HttpGet("relationship-kinds")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> GetRelationshipKinds(CancellationToken ct)
    {
        var kinds = await _relationshipService.GetKindsAsync(ct);
        return Ok(kinds);
    }

    // Danh sách TOÀN BỘ quan hệ cho trang quản lý (mỗi cặp một dòng). Route literal — không đụng {id}.
    [HttpGet("relationships")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> SearchRelationships([FromQuery] RelationshipSearchRequest request, CancellationToken ct)
    {
        var result = await _relationshipService.SearchAllAsync(request, GetActorUserId(), ct);
        return Ok(result);
    }

    [HttpGet("{id}/relationships")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> GetRelationships(long id, CancellationToken ct)
    {
        var items = await _relationshipService.GetForCustomerAsync(id, GetActorUserId(), ct);
        return Ok(items);
    }

    [HttpPost("{id}/relationships")]
    [RequirePermission(PermissionCodes.CustomerRelationshipManage, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> CreateRelationship(long id, [FromBody] CreateCustomerRelationshipRequest request, CancellationToken ct)
    {
        var dto = await _relationshipService.CreateAsync(id, request, GetActorUserId(), ct);
        return CreatedAtAction(nameof(GetRelationships), new { id }, dto);
    }

    [HttpDelete("{id}/relationships/{relationshipId}")]
    [RequirePermission(PermissionCodes.CustomerRelationshipManage, PermissionScope.ServiceFiltered)]
    public async Task<IActionResult> DeleteRelationship(long id, long relationshipId, CancellationToken ct)
    {
        await _relationshipService.DeleteAsync(id, relationshipId, GetActorUserId(), ct);
        return NoContent();
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
