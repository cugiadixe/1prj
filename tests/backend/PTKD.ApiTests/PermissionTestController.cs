using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.ApiTests;

[ApiController]
[Route("api/v2/test/[controller]")]
public class PermissionTestController : ControllerBase
{
    [HttpGet("company-scoped")]
    [Authorize]
    [RequirePermission("TEST_COMPANY_PERM", PermissionScope.Company)]
    public IActionResult GetCompanyScoped()
    {
        return Ok("Company protected data");
    }

    [HttpGet("global-scoped")]
    [Authorize]
    [RequirePermission("TEST_GLOBAL_PERM", PermissionScope.Global)]
    public IActionResult GetGlobalScoped()
    {
        return Ok("Global protected data");
    }
}
