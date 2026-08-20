using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.CardWatermarks;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/card-watermarks")]
[Authorize]
public class CardWatermarksController : ControllerBase
{
    private readonly ICardWatermarkService _service;

    public CardWatermarksController(ICardWatermarkService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.CardWatermarkManage, PermissionScope.Company)]
    public async Task<IActionResult> GetAll([FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
        => Ok(await _service.ListAsync(companyId, ct));

    [HttpPost]
    [RequirePermission(PermissionCodes.CardWatermarkManage, PermissionScope.Company)]
    [RequestSizeLimit(4_000_000)]
    public async Task<IActionResult> Upload(
        [FromHeader(Name = "X-Company-Id")] long companyId,
        IFormFile file,
        [FromForm] string name,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Thiếu file", Detail = "Vui lòng chọn ảnh hoa văn." });

        using var ms = new System.IO.MemoryStream();
        await file.CopyToAsync(ms, ct);
        var dto = await _service.UploadAsync(companyId, name, file.ContentType, ms.ToArray(), GetActorUserId(), ct);
        return Ok(dto);
    }

    [HttpGet("{id}/content")]
    [RequirePermission(PermissionCodes.CardWatermarkManage, PermissionScope.Company)]
    public async Task<IActionResult> Content(long id, [FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        var content = await _service.GetContentAsync(id, companyId, ct);
        if (content == null) return NotFound();
        return File(content.Bytes, content.ContentType);
    }

    [HttpDelete("{id}")]
    [RequirePermission(PermissionCodes.CardWatermarkManage, PermissionScope.Company)]
    public async Task<IActionResult> Delete(long id, [FromHeader(Name = "X-Company-Id")] long companyId, CancellationToken ct)
    {
        await _service.DeleteAsync(id, companyId, GetActorUserId(), ct);
        return NoContent();
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return long.Parse(claim!);
    }
}
