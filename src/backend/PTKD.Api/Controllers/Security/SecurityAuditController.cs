using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Common.Models;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Audit.DTOs;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Controllers.Security;

/// <summary>
/// Read-only API for Security Audit Events.
/// Enforces SECURITY_AUDIT_VIEW at GLOBAL scope.
/// </summary>
[ApiController]
[Authorize]
[RequirePermission(PermissionCodes.SecurityAuditView, PermissionScope.Global)]
[Route("api/v2/security/audit-events")]
public sealed class SecurityAuditController : ControllerBase
{
    private readonly ISecurityAuditQueryService _queryService;

    public SecurityAuditController(ISecurityAuditQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SecurityAuditEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditEvents(
        [FromQuery] SecurityAuditQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        // Validate parameters locally — audit-specific codes are not registered in GlobalExceptionFilter.
        if (parameters.Page < 1)
            return AuditValidationProblem("INVALID_PAGE", "Page must be greater than or equal to 1.");
        if (parameters.PageSize < 1)
            return AuditValidationProblem("INVALID_PAGE_SIZE", "PageSize must be greater than or equal to 1.");
        if (parameters.PageSize > 1000)
            return AuditValidationProblem("PAGE_SIZE_EXCEEDED", "PageSize cannot exceed 1000.");
        if (parameters.FromUtc.HasValue && parameters.ToUtc.HasValue && parameters.FromUtc > parameters.ToUtc)
            return AuditValidationProblem("INVALID_DATE_RANGE", "FromUtc must be less than or equal to ToUtc.");

        var result = await _queryService.GetAuditEventsAsync(parameters, cancellationToken);
        return Ok(result);
    }

    private ObjectResult AuditValidationProblem(string errorCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Detail = detail,
            Type = "https://ptkd-erp.internal/docs/errors/business-rule"
        };
        problem.Extensions["errorCode"] = errorCode;
        return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
    }
}
