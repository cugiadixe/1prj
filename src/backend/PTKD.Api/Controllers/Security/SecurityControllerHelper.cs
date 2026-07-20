using System.Security.Claims;
using PTKD.Application.Common.Exceptions;

namespace PTKD.Api.Controllers.Security;

/// <summary>
/// Provides controller helpers for extracting the authenticated user's identity
/// and performing manual permission checks per OD-D-B-02.
/// </summary>
internal static class SecurityControllerHelper
{
    /// <summary>
    /// Extracts the userId from the authenticated JWT token's "sub" claim.
    /// Throws if the token is missing or malformed.
    /// </summary>
    public static long GetActorUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException("Token is missing the user identity claim.");

        if (!long.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException("Token contains an invalid user identity claim.");

        return userId;
    }

    /// <summary>
    /// Throws PermissionDeniedException (403) if the permission check returns false.
    /// Derive the actor from the JWT; never trust client-supplied actor IDs.
    /// </summary>
    public static async Task EnforcePermissionAsync(
        PTKD.Application.Security.Authorization.Interfaces.IPermissionEvaluator evaluator,
        long userId,
        string permissionCode,
        long? companyId,
        CancellationToken ct = default)
    {
        var allowed = await evaluator.EvaluateAsync(userId, permissionCode, companyId, ct);
        if (!allowed)
            throw new PermissionDeniedException(
                "SEC_PERMISSION_DENIED",
                $"User {userId} does not have permission '{permissionCode}'.");
    }
}
