using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Graves.DTOs;
using PTKD.Application.Graves.Services;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/graves")]
[Authorize]
public class GravesController : ControllerBase
{
    private readonly IGraveService _graveService;
    private readonly IGraveAttachmentService _attachmentService;

    public GravesController(IGraveService graveService, IGraveAttachmentService attachmentService)
    {
        _graveService = graveService;
        _attachmentService = attachmentService;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.GraveView, PermissionScope.Global)]
    public async Task<IActionResult> Search([FromQuery] GraveSearchRequest request, CancellationToken ct)
    {
        var result = await _graveService.SearchGravesAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [RequirePermission(PermissionCodes.GraveView, PermissionScope.Global)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var grave = await _graveService.GetGraveByIdAsync(id, ct);
        if (grave == null) return NotFound();
        return Ok(grave);
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.GraveCreate, PermissionScope.Global)]
    public async Task<IActionResult> Create([FromBody] CreateGraveRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var grave = await _graveService.CreateGraveAsync(request, actorUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = grave.Id }, grave);
    }

    [HttpPut("{id}")]
    [RequirePermission(PermissionCodes.GraveUpdate, PermissionScope.Global)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateGraveRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var grave = await _graveService.UpdateGraveAsync(id, request, actorUserId, ct);
        return Ok(grave);
    }

    [HttpPost("{id}/occupants")]
    [RequirePermission(PermissionCodes.GraveUpdate, PermissionScope.Global)]
    public async Task<IActionResult> AddOccupant(long id, [FromBody] CreateGraveOccupantRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var occupant = await _graveService.AddOccupantAsync(id, request, actorUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id }, occupant);
    }

    [HttpPut("{id}/occupants/{occupantId}")]
    [RequirePermission(PermissionCodes.GraveUpdate, PermissionScope.Global)]
    public async Task<IActionResult> UpdateOccupant(long id, long occupantId, [FromBody] UpdateGraveOccupantRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var occupant = await _graveService.UpdateOccupantAsync(id, occupantId, request, actorUserId, ct);
        return Ok(occupant);
    }

    // ─── Liên hệ khẩn cấp động ───

    [HttpPost("{id}/emergency-contacts")]
    [RequirePermission(PermissionCodes.GraveUpdate, PermissionScope.Global)]
    public async Task<IActionResult> AddEmergencyContact(long id, [FromBody] CreateGraveEmergencyContactRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var contact = await _graveService.AddEmergencyContactAsync(id, request, actorUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id }, contact);
    }

    [HttpPut("{id}/emergency-contacts/{contactId}")]
    [RequirePermission(PermissionCodes.GraveUpdate, PermissionScope.Global)]
    public async Task<IActionResult> UpdateEmergencyContact(long id, long contactId, [FromBody] UpdateGraveEmergencyContactRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var contact = await _graveService.UpdateEmergencyContactAsync(id, contactId, request, actorUserId, ct);
        return Ok(contact);
    }

    [HttpDelete("{id}/emergency-contacts/{contactId}")]
    [RequirePermission(PermissionCodes.GraveUpdate, PermissionScope.Global)]
    public async Task<IActionResult> RemoveEmergencyContact(long id, long contactId, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        await _graveService.RemoveEmergencyContactAsync(id, contactId, actorUserId, ct);
        return NoContent();
    }

    [HttpPost("{id}/transfer-owner")]
    [RequirePermission(PermissionCodes.GraveTransferOwnership, PermissionScope.Global)]
    public async Task<IActionResult> TransferOwner(long id, [FromBody] TransferOwnershipRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var result = await _graveService.TransferOwnershipAsync(id, request, actorUserId, ct);
        return Ok(result);
    }

    [HttpPost("owner-death")]
    [RequirePermission(PermissionCodes.GraveTransferOwnership, PermissionScope.Global)]
    public async Task<IActionResult> ProcessOwnerDeath([FromBody] OwnerDeathRequest request, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        var result = await _graveService.ProcessOwnerDeathAsync(request, actorUserId, ct);
        return Ok(result);
    }

    [HttpGet("{id}/ownership-history")]
    [RequirePermission(PermissionCodes.GraveView, PermissionScope.Global)]
    public async Task<IActionResult> GetOwnershipHistory(long id, CancellationToken ct)
    {
        var history = await _graveService.GetOwnershipHistoryAsync(id, ct);
        return Ok(history);
    }

    // ─── Ảnh / tài liệu đính kèm ───

    [HttpGet("{id}/attachments")]
    [RequirePermission(PermissionCodes.GraveView, PermissionScope.Global)]
    public async Task<IActionResult> ListAttachments(long id, CancellationToken ct)
    {
        var items = await _attachmentService.ListAsync(id, ct);
        return Ok(items);
    }

    [HttpPost("{id}/attachments")]
    [RequirePermission(PermissionCodes.GraveAttachmentManage, PermissionScope.Global)]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> UploadAttachment(
        long id,
        IFormFile file,
        [FromForm] string category,
        [FromForm] string? description,
        [FromForm] long? ownershipHistoryId,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Thiếu file", Detail = "Vui lòng chọn file để tải lên." });

        var actorUserId = GetActorUserId();
        await using var stream = file.OpenReadStream();
        var dto = await _attachmentService.UploadAsync(
            id, category, ownershipHistoryId, file.FileName, file.ContentType, file.Length, stream, description, actorUserId, ct);
        return Ok(dto);
    }

    [HttpGet("{id}/attachments/{attachmentId}/content")]
    [RequirePermission(PermissionCodes.GraveView, PermissionScope.Global)]
    public async Task<IActionResult> GetAttachmentContent(long id, long attachmentId, [FromQuery] bool thumbnail, CancellationToken ct)
    {
        var content = await _attachmentService.OpenContentAsync(attachmentId, thumbnail, ct);
        if (content == null) return NotFound();
        return File(content.Stream, content.ContentType);
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    [RequirePermission(PermissionCodes.GraveAttachmentManage, PermissionScope.Global)]
    public async Task<IActionResult> DeleteAttachment(long id, long attachmentId, CancellationToken ct)
    {
        var actorUserId = GetActorUserId();
        await _attachmentService.DeleteAsync(attachmentId, actorUserId, ct);
        return NoContent();
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
