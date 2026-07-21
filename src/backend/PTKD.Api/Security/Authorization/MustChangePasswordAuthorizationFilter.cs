using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PTKD.Api.Security.Authorization;

public class MustChangePasswordAuthorizationFilter : IAsyncAuthorizationFilter
{
    private static readonly string[] AllowedPaths = new[]
    {
        "/api/v2/auth/change-password",
        "/api/v2/auth/logout",
        "/api/v2/auth/refresh"
    };

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint == null)
        {
            return Task.CompletedTask;
        }

        // If endpoint allows anonymous, skip
        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            return Task.CompletedTask;
        }

        var user = context.HttpContext.User;
        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            return Task.CompletedTask;
        }

        var mustChange = user.HasClaim(c => c.Type == "must_change_password" && c.Value == "true");
        if (!mustChange)
        {
            return Task.CompletedTask;
        }

        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        
        if (AllowedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.CompletedTask;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = 403,
            Title = "Forbidden",
            Detail = "You must change your password before accessing this endpoint.",
            Type = "https://ptkd-erp.internal/errors/auth/must-change-password"
        })
        {
            StatusCode = 403
        };

        return Task.CompletedTask;
    }
}
