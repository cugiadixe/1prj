using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PTKD.Api.Auth.Models;
using PTKD.Api.Security;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Models;
using PTKD.Application.Security.Authentication.Services;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authorization.DTOs;

namespace PTKD.Api.Controllers;

[ApiController]
[Route("api/v2/auth")]
public sealed class AuthController : ControllerBase
{
    private const string RefreshCookieName = "RefreshToken";
    private const string RefreshCookiePath = "/api/v2/auth";
    private const int RefreshTokenLifetimeDays = 7;
    private const int AccessTokenLifetimeSeconds = 900; // 15 minutes

    private readonly IAuthenticationAccountService _authService;
    private readonly ITokenSessionLifecycleService _sessionService;
    private readonly CsrfTokenService _csrfService;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ISecurityAdminService _securityAdminService;

    public AuthController(
        IAuthenticationAccountService authService,
        ITokenSessionLifecycleService sessionService,
        CsrfTokenService csrfService,
        IPermissionEvaluator permissionEvaluator,
        ISecurityAdminService securityAdminService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _csrfService = csrfService ?? throw new ArgumentNullException(nameof(csrfService));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
        _securityAdminService = securityAdminService ?? throw new ArgumentNullException(nameof(securityAdminService));
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
                DisplayName: null),
            MustChangePassword: sessionResult.MustChangePassword);

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
            User: new LoginUserInfo(UserId: 0, Username: string.Empty, DisplayName: null),
            MustChangePassword: sessionResult.MustChangePassword);

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

    /// <summary>
    /// POST /api/v2/auth/change-password
    /// Allows a user to change their password.
    /// Requires an active session. Revokes all current sessions upon success.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var accountIdString = User.FindFirst("auth_account_id")?.Value;
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        var username = User.FindFirst("login_name")?.Value;

        if (string.IsNullOrEmpty(accountIdString) || !long.TryParse(accountIdString, out var accountId) ||
            string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId) ||
            string.IsNullOrEmpty(username))
        {
            return Unauthorized(BuildGenericAuthProblem());
        }

        if (!_csrfService.Validate(Request))
        {
            return Forbid403(BuildCsrfProblem());
        }

        // 1. Verify current password safely without login side effects
        var authResult = await _authService.VerifyCurrentPasswordAsync(
            username, request.CurrentPassword, cancellationToken);

        if (!authResult.IsSuccess || authResult.RowVersion == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid Current Password",
                Detail = "The current password provided is incorrect.",
                Type = "https://ptkd-erp.internal/docs/errors/auth/invalid-current-password"
            });
        }

        // 2. Change password
        var command = new ChangePasswordCommand(
            accountId,
            request.CurrentPassword,
            request.NewPassword,
            authResult.RowVersion,
            userId);

        var result = await _authService.ChangePasswordAsync(command, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorCode == AuthenticationErrorCodes.PasswordReuse ||
                result.ErrorCode == AuthenticationErrorCodes.PasswordLengthInvalid ||
                result.ErrorCode == AuthenticationErrorCodes.PasswordContainsProviderSubject)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Invalid New Password",
                    Detail = "The new password does not meet policy requirements.",
                    Type = $"https://ptkd-erp.internal/docs/errors/auth/{result.ErrorCode.ToLowerInvariant()}"
                });
            }

            if (result.ErrorCode == AuthenticationErrorCodes.AccountConcurrencyConflict)
            {
                return Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Concurrency Conflict",
                    Detail = "The account was modified by another request.",
                    Type = "https://ptkd-erp.internal/docs/errors/auth/concurrency-conflict"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Password Change Failed",
                Detail = "The password could not be changed.",
                Type = "https://ptkd-erp.internal/docs/errors/auth/password-change-failed"
            });
        }

        // 3. Clear cookies (require re-login)
        DeleteRefreshCookie();
        _csrfService.Delete(Response);

        return NoContent();
    }

    /// <summary>
    /// GET /api/v2/auth/me/permissions
    /// Returns the current user's effective permissions.
    /// </summary>
    [HttpGet("me/permissions")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(CurrentUserPermissionsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUserPermissions(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out var userId))
        {
            return Unauthorized(BuildGenericAuthProblem());
        }

        long? parsedCompanyId = null;
        var headerValue = Request.Headers["X-Company-Id"].ToString();

        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            if (!long.TryParse(headerValue, out var cid))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Malformed Company Context",
                    Detail = "The X-Company-Id header must be a valid integer.",
                    Type = "https://ptkd-erp.example.com/errors/malformed-company-context"
                });
            }
            parsedCompanyId = cid;
        }

        var effectiveCodes = await _permissionEvaluator.GetEffectivePermissionsAsync(userId, parsedCompanyId, cancellationToken);
        var catalog = await _securityAdminService.ListPermissionsAsync(cancellationToken);

        var dtos = new List<CurrentUserPermissionDto>();
        foreach (var code in effectiveCodes)
        {
            var cat = catalog.FirstOrDefault(c => c.PermissionCode == code);
            if (cat != null)
            {
                dtos.Add(new CurrentUserPermissionDto(
                    cat.PermissionCode,
                    cat.DataScope,
                    cat.DataScope == "COMPANY" ? parsedCompanyId : null));
            }
        }

        return Ok(new CurrentUserPermissionsResponseDto(dtos));
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

        if (result.Outcome == AuthenticationAttemptOutcome.AccountLocked)
        {
            return Forbid403(new ProblemDetails
            {
                Status = 403,
                Title = "Access Denied",
                Detail = "The account is not accessible.",
                Type = "https://ptkd-erp.internal/docs/errors/auth/access-denied"
            });
        }

        // Use 401 for all other auth failures (generic, non-enumerating).
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
