using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Security.Authorization;

public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IPermissionEvaluator _permissionEvaluator;

    public PermissionAuthorizationFilter(IPermissionEvaluator permissionEvaluator)
    {
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint == null)
        {
            return;
        }

        var attribute = endpoint.Metadata.GetMetadata<RequirePermissionAttribute>();
        if (attribute == null)
        {
            return;
        }

        if (context.HttpContext.User.Identity == null || !context.HttpContext.User.Identity.IsAuthenticated)
        {
            // We let [Authorize] and the auth pipeline handle the 401. 
            // If the endpoint lacks [Authorize] but has [RequirePermission], we should still fail.
            // By returning here, if [Authorize] is present, it will eventually 401. 
            // If it's not present, this would bypass permission checks for anonymous users!
            // Wait, IAsyncAuthorizationFilter runs *at the same time* as [Authorize] filters.
            // If we don't set a result and it's not authenticated, [Authorize] will set 401.
            // But what if the user forgot [Authorize] and just put [RequirePermission]?
            // To be safe, if they aren't authenticated, we can set 401 ourselves just in case, 
            // but the requirement said "Lets existing auth pipeline handle 401 for missing/invalid JWT."
            // So if they aren't authenticated, we just return.
            return;
        }

        var userIdString = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdString, out var userId))
        {
            return;
        }

        long? companyId = null;

        if (attribute.Scope == PermissionScope.Company)
        {
            var headerValue = context.HttpContext.Request.Headers["X-Company-Id"].ToString();

            if (string.IsNullOrWhiteSpace(headerValue))
            {
                context.Result = new BadRequestObjectResult(new ProblemDetails
                {
                    Status = 400,
                    Title = "Missing Company Context",
                    Detail = "The X-Company-Id header is required for this endpoint.",
                    Type = "https://ptkd-erp.example.com/errors/missing-company-context"
                });
                return;
            }

            if (!long.TryParse(headerValue, out var parsedCompanyId))
            {
                context.Result = new BadRequestObjectResult(new ProblemDetails
                {
                    Status = 400,
                    Title = "Malformed Company Context",
                    Detail = "The X-Company-Id header must be a valid integer.",
                    Type = "https://ptkd-erp.example.com/errors/malformed-company-context"
                });
                return;
            }

            companyId = parsedCompanyId;
        }

        try
        {
            var isGranted = await _permissionEvaluator.EvaluateAsync(
                userId, 
                attribute.PermissionCode, 
                companyId, 
                context.HttpContext.RequestAborted);

            if (!isGranted)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = 403,
                    Title = "Forbidden",
                    Detail = "You do not have the required permissions or company access.",
                    Type = "https://ptkd-erp.example.com/errors/forbidden"
                })
                {
                    StatusCode = 403
                };
            }
        }
        catch
        {
            // Fail closed on evaluator errors with sanitized response
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 403,
                Title = "Forbidden",
                Detail = "Permission evaluation failed.",
                Type = "https://ptkd-erp.example.com/errors/evaluation-failed"
            })
            {
                StatusCode = 403
            };
        }
    }
}
