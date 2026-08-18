using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Dashboard;
using PTKD.Application.Dashboard.DTOs;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    // Scope Company: cổng kiểm quyền BẮT BUỘC người gọi thật sự thuộc công ty đang khai qua
    // header X-Company-Id (IsMemberOfAsync) VÀ có quyền ở công ty đó, trước khi service đọc số
    // liệu. Không có chốt này thì bất kỳ ai đăng nhập cũng đổi header để đọc doanh thu công ty khác.
    [HttpGet("summary")]
    [RequirePermission(PermissionCodes.CustomerViewBasic, PermissionScope.Company)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromHeader(Name = "X-Company-Id")] long companyId,
        CancellationToken ct)
    {
        var result = await _service.GetSummaryAsync(companyId, ct);
        return Ok(result);
    }
}
