using Microsoft.AspNetCore.Mvc;
using System;

namespace PTKD.Api.Controllers
{
    [ApiController]
    [Route("api/v2/system")]
    public class SystemController : ControllerBase
    {
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            return Ok(new
            {
                appName = "PTKD ERP",
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
            });
        }
    }
}
