using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PTKD.ApiTests;

[ApiController]
[Route("api/v2/test/[controller]")]
public class ProtectedTestController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult Get()
    {
        return Ok("Protected data");
    }
}
