using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Security.Authentication.Models;

namespace PTKD.Application.Security.Authentication.Interfaces;

public interface ITokenSessionLifecycleService
{
    /// <summary>
    /// Creates a new token session after successful authentication.
    /// </summary>
    Task<TokenSessionResult> CreateSessionAsync(
        long accountId,
        string username,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an existing session using a refresh token.
    /// </summary>
    Task<TokenSessionResult> RefreshSessionAsync(
        string refreshTokenMaterial,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current family/session from a refresh token.
    /// </summary>
    Task<LogoutResult> LogoutAsync(
        string refreshTokenMaterial,
        CancellationToken cancellationToken = default);
}
