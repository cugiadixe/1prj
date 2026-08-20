using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Relationships.DTOs;
using PTKD.Application.Relationships.Services;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.API.Controllers;

/// <summary>
/// Quản lý DANH MỤC loại quan hệ gia đình (Relationship_Kinds) — cấu hình hệ thống dùng chung.
/// </summary>
[ApiController]
[Route("api/v2/relationship-kinds")]
[Authorize]
public class RelationshipKindsController : ControllerBase
{
    private readonly IRelationshipKindService _service;

    public RelationshipKindsController(IRelationshipKindService service)
    {
        _service = service;
    }

    // XEM danh mục cần quyền VIEW — nhẹ hơn MANAGE để nhân viên tải được dropdown khi khai quan hệ.
    [HttpGet]
    [RequirePermission(PermissionCodes.RelationshipKindView, PermissionScope.Global)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var kinds = await _service.GetAllAsync(ct);
        return Ok(kinds);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.RelationshipKindManage, PermissionScope.Global)]
    public async Task<IActionResult> Create([FromBody] CreateRelationshipKindRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateAsync(request, GetActorUserId(), ct);
        return CreatedAtAction(nameof(GetAll), null, dto);
    }

    [HttpPut("{kindCode}")]
    [RequirePermission(PermissionCodes.RelationshipKindManage, PermissionScope.Global)]
    public async Task<IActionResult> Update(string kindCode, [FromBody] UpdateRelationshipKindRequest request, CancellationToken ct)
    {
        await _service.UpdateAsync(kindCode, request, GetActorUserId(), ct);
        return NoContent();
    }

    [HttpDelete("{kindCode}")]
    [RequirePermission(PermissionCodes.RelationshipKindManage, PermissionScope.Global)]
    public async Task<IActionResult> Delete(string kindCode, CancellationToken ct)
    {
        await _service.DeleteAsync(kindCode, GetActorUserId(), ct);
        return NoContent();
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
