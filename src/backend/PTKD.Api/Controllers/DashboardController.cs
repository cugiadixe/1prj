using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Dashboard;
using PTKD.Application.Dashboard.DTOs;

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

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromHeader(Name = "X-Company-Id")] long companyId,
        CancellationToken ct)
    {
        var result = await _service.GetSummaryAsync(companyId, ct);
        return Ok(result);
    }
}
