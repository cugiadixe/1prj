using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Auth.Models;
using PTKD.Api.Security;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Models;
using PTKD.Application.Security.Authentication.Services;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/auth")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshCookieName = "__Host-RefreshToken";
    private const string RefreshCookiePath = "/api/v2/auth";
    private const int RefreshTokenLifetimeDays = 7;
    private const int AccessTokenLifetimeSeconds = 900; // 15 minutes

    private readonly IAuthenticationAccountService _authService;
    private readonly ITokenSessionLifecycleService _sessionService;
    private readonly CsrfTokenService _csrfService;

    public AuthController(
        IAuthenticationAccountService authService,
        ITokenSessionLifecycleService sessionService,
        CsrfTokenService csrfService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _csrfService = csrfService ?? throw new ArgumentNullException(nameof(csrfService));
    }

    /// <summary>
    /// POST /api/v2/auth/login
    /// Authenticates a user and issues JWT access token + refresh token cookie + CSRF token.
    /// Does not require CSRF validation (login is not cookie-reliant).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(BuildGenericAuthProblem());
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var authResult = await _authService.AuthenticateAsync(
            new AuthenticateAccountCommand("INTERNAL", request.Username.Trim().ToUpperInvariant(), request.Password),
            cancellationToken);

        if (!authResult.IsSuccess)
        {
            return MapAuthFailureToProblem(authResult);
        }

        var sessionResult = await _sessionService.CreateSessionAsync(
            authResult.AccountId!.Value,
            request.Username,
            ipAddress,
            userAgent,
            cancellationToken);

        if (!sessionResult.IsSuccess)
        {
            return MapSessionFailureToProblem(sessionResult);
        }

        SetRefreshCookie(sessionResult.RefreshTokenMaterial!, sessionResult.RefreshTokenExpiresAtUtc!.Value);
        _csrfService.Issue(Response);

        // Build login response — DO NOT include RefreshTokenMaterial
        var response = new LoginResponse(
            AccessToken: sessionResult.AccessToken!,
            TokenType: "Bearer",
            ExpiresIn: AccessTokenLifetimeSeconds,
            ExpiresAtUtc: sessionResult.AccessTokenExpiresAtUtc!.Value,
            User: new LoginUserInfo(
                UserId: authResult.UserId!.Value,
                Username: request.Username,
                DisplayName: null));  // DisplayName resolved from User.FullName if available

        return Ok(response);
    }

    /// <summary>
    /// POST /api/v2/auth/refresh
    /// Rotates refresh token cookie. Requires CSRF validation.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!_csrfService.Validate(Request))
        {
            return Forbid403(BuildCsrfProblem());
        }

        var refreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(BuildGenericRefreshProblem());
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var sessionResult = await _sessionService.RefreshSessionAsync(
            refreshToken, ipAddress, userAgent, cancellationToken);

        if (!sessionResult.IsSuccess)
        {
            // On any failure, clear the cookies to force re-login
            DeleteRefreshCookie();
            _csrfService.Delete(Response);
            return MapSessionFailureToProblem(sessionResult);
        }

        // Rotate: set new refresh cookie and new CSRF token
        SetRefreshCookie(sessionResult.RefreshTokenMaterial!, sessionResult.RefreshTokenExpiresAtUtc!.Value);
        _csrfService.Issue(Response);

        var response = new LoginResponse(
            AccessToken: sessionResult.AccessToken!,
            TokenType: "Bearer",
            ExpiresIn: AccessTokenLifetimeSeconds,
            ExpiresAtUtc: sessionResult.AccessTokenExpiresAtUtc!.Value,
            User: new LoginUserInfo(UserId: 0, Username: string.Empty, DisplayName: null));

        return Ok(response);
    }

    /// <summary>
    /// POST /api/v2/auth/logout
    /// Revokes current family/session. Requires CSRF. Returns generic success.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];

        // Only validate CSRF when a refresh cookie is present (cookie-reliant operation)
        if (!string.IsNullOrEmpty(refreshToken) && !_csrfService.Validate(Request))
        {
            return Forbid403(BuildCsrfProblem());
        }

        if (!string.IsNullOrEmpty(refreshToken))
        {
            // Attempt revocation — generic success regardless of token state (non-enumeration)
            await _sessionService.LogoutAsync(refreshToken, cancellationToken);
        }

        // Always clear cookies
        DeleteRefreshCookie();
        _csrfService.Delete(Response);

        return NoContent();
    }

    // ── Cookie helpers ────────────────────────────────────────────────────

    private void SetRefreshCookie(string token, DateTime expires)
    {
        // __Host- prefix enforces: Secure=true, Path=/, no Domain
        // We use our own path enforcement separately since /api/v2/auth != /
        // So use plain name with explicit Secure + Path attributes.
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = expires
        });
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath
        });
    }

    // ── ProblemDetails helpers ────────────────────────────────────────────

    private IActionResult MapAuthFailureToProblem(AuthenticationAttemptResult result)
    {
        // AUTH_ACCOUNT_LOCKED → 403 generic
        if (result.Outcome == AuthenticationAttemptOutcome.InfrastructureFailure)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = 503,
                Title = "Service Unavailable",
                Detail = "Authentication service is temporarily unavailable.",
                Type = "https://ptkd-erp.internal/docs/errors/auth/service-unavailable"
            });
        }

        // Determine if locked (generic mapping: any locked state → 403)
        // We cannot distinguish externally — return 401 for all invalid credential cases
        // to maintain non-enumeration, UNLESS the service returned a lockout signal.
        // Since AuthenticationAttemptResult.Outcome doesn't have a Locked enum value,
        // we check InternalReason. But ErrorCode may be null for InvalidCredentials.
        // Use 401 for all auth failures except infra (generic, non-enumerating).
        return Unauthorized(BuildGenericAuthProblem());
    }

    private IActionResult MapSessionFailureToProblem(TokenSessionResult result)
    {
        return result.Status switch
        {
            TokenSessionStatus.AccountLocked =>
                Forbid403(new ProblemDetails
                {
                    Status = 403,
                    Title = "Access Denied",
                    Detail = "The account is not accessible.",
                    Type = "https://ptkd-erp.internal/docs/errors/auth/access-denied"
                }),
            _ => Unauthorized(BuildGenericRefreshProblem())
        };
    }

    private static ProblemDetails BuildGenericAuthProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Authentication Failed",
        Detail = "Invalid credentials or account not eligible.",
        Type = "https://ptkd-erp.internal/docs/errors/auth/invalid-credentials"
    };

    private static ProblemDetails BuildGenericRefreshProblem() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Authentication Failed",
        Detail = "Session is invalid or has expired.",
        Type = "https://ptkd-erp.internal/docs/errors/auth/session-invalid"
    };

    private static ProblemDetails BuildCsrfProblem() => new()
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "CSRF Validation Failed",
        Detail = "The CSRF token is missing or invalid.",
        Type = "https://ptkd-erp.internal/docs/errors/auth/csrf-invalid"
    };

    private ObjectResult Forbid403(ProblemDetails problem) =>
        StatusCode(StatusCodes.Status403Forbidden, problem);
}
