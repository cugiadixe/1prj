using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;
using System;

namespace PTKD.Api.Controllers
{
    [ApiController]
    [Route("api/v2/system")]
    [Authorize]
    [RequirePermission(PermissionCodes.SecurityAdminManage, PermissionScope.Global)]
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
