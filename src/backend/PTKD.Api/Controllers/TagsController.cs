using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Application.Tags.DTOs;
using PTKD.Application.Tags.Services;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    /// <summary>Danh mục thẻ theo loại. Mọi người dùng đăng nhập đều đọc được (phục vụ picker/bộ lọc).</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string type, [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var tags = await _tagService.ListTagsAsync(type, includeInactive, ct);
        return Ok(tags);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.TagManage, PermissionScope.Global)]
    public async Task<IActionResult> Create([FromBody] CreateTagRequest request, CancellationToken ct)
    {
        var tag = await _tagService.CreateTagAsync(request, GetActorUserId(), ct);
        return Ok(tag);
    }

    [HttpPut("{id}")]
    [RequirePermission(PermissionCodes.TagManage, PermissionScope.Global)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateTagRequest request, CancellationToken ct)
    {
        var tag = await _tagService.UpdateTagAsync(id, request, GetActorUserId(), ct);
        return Ok(tag);
    }

    [HttpDelete("{id}")]
    [RequirePermission(PermissionCodes.TagManage, PermissionScope.Global)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        await _tagService.DeactivateTagAsync(id, GetActorUserId(), ct);
        return NoContent();
    }

    /// <summary>Đặt lại toàn bộ tập thẻ của một khách hàng.</summary>
    [HttpPut("customer/{customerId}")]
    [RequirePermission(PermissionCodes.TagManage, PermissionScope.Global)]
    public async Task<IActionResult> SetCustomerTags(long customerId, [FromBody] SetEntityTagsRequest request, CancellationToken ct)
    {
        var tags = await _tagService.SetCustomerTagsAsync(customerId, request, GetActorUserId(), ct);
        return Ok(tags);
    }

    /// <summary>Đặt lại toàn bộ tập thẻ của một phần mộ.</summary>
    [HttpPut("grave/{graveId}")]
    [RequirePermission(PermissionCodes.TagManage, PermissionScope.Global)]
    public async Task<IActionResult> SetGraveTags(long graveId, [FromBody] SetEntityTagsRequest request, CancellationToken ct)
    {
        var tags = await _tagService.SetGraveTagsAsync(graveId, request, GetActorUserId(), ct);
        return Ok(tags);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
