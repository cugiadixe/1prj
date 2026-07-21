using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Security.AccountManagement;
using PTKD.Application.Security.AccountManagement.DTOs;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.Api.Controllers.Security;

[ApiController]
[Authorize]
[RequirePermission(PermissionCodes.SecurityAccountManage, PermissionScope.Global)]
[Route("api/v2/security/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountManagementService _service;

    public AccountsController(IAccountManagementService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("{accountId:long}")]
    [ProducesResponseType(typeof(AccountDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAccountDetail(long accountId, CancellationToken cancellationToken)
    {
        var detail = await _service.GetAccountDetailAsync(accountId, cancellationToken);
        if (detail is null)
            return NotFoundProblem("AUTH_ACCOUNT_NOT_FOUND", "Account not found.");

        return Ok(detail);
    }

    [HttpPost("{accountId:long}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Activate(long accountId, CancellationToken cancellationToken)
    {
        var actorUserId = SecurityControllerHelper.GetActorUserId(User);
        var result = await _service.ActivateAccountAsync(accountId, actorUserId, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("{accountId:long}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Disable(long accountId, [FromBody] AccountReasonRequest request, CancellationToken cancellationToken)
    {
        var reasonError = ValidateReason(request.Reason, "disable");
        if (reasonError is not null) return reasonError;

        var actorUserId = SecurityControllerHelper.GetActorUserId(User);
        var result = await _service.DisableAccountAsync(accountId, request.Reason.Trim(), actorUserId, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("{accountId:long}/lock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Lock(long accountId, [FromBody] AccountReasonRequest request, CancellationToken cancellationToken)
    {
        var reasonError = ValidateReason(request.Reason, "lock");
        if (reasonError is not null) return reasonError;

        var actorUserId = SecurityControllerHelper.GetActorUserId(User);
        var result = await _service.LockAccountAsync(accountId, request.Reason.Trim(), actorUserId, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("{accountId:long}/unlock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Unlock(long accountId, CancellationToken cancellationToken)
    {
        var actorUserId = SecurityControllerHelper.GetActorUserId(User);
        var result = await _service.UnlockAccountAsync(accountId, actorUserId, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("{accountId:long}/reset-password")]
    [ProducesResponseType(typeof(AdminResetPasswordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResetPassword(long accountId, [FromBody] AccountReasonRequest request, CancellationToken cancellationToken)
    {
        var reasonError = ValidateReason(request.Reason, "password reset");
        if (reasonError is not null) return reasonError;

        var actorUserId = SecurityControllerHelper.GetActorUserId(User);
        var result = await _service.AdminResetPasswordAsync(accountId, request.Reason.Trim(), actorUserId, cancellationToken);

        if (!result.Succeeded)
            return MapErrorResult(result);

        // Return temporary password exactly once; caller must relay to admin out-of-band.
        return Ok(new AdminResetPasswordDto(result.TemporaryPassword!));
    }

    [HttpPost("{accountId:long}/revoke-sessions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeSessions(long accountId, [FromBody] AccountReasonRequest request, CancellationToken cancellationToken)
    {
        var reasonError = ValidateReason(request.Reason, "session revocation");
        if (reasonError is not null) return reasonError;

        var actorUserId = SecurityControllerHelper.GetActorUserId(User);
        var result = await _service.RevokeAllSessionsAsync(accountId, request.Reason.Trim(), actorUserId, cancellationToken);
        return MapResult(result);
    }

    // ── Reason validation ─────────────────────────────────────────────────────

    private const int MaxReasonLength = 500;

    private static readonly Regex SensitiveReasonPattern = new(
        @"\b(password|temporaryPassword|temporary.password|temp.password|token|refresh.token|secret|hash|security.stamp|SecurityStamp|security_stamp|private.key|signing.key|api.key|access.key)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private IActionResult? ValidateReason(string? reason, string operationName)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ValidationProblem($"Reason is required for {operationName}.", "REASON_REQUIRED");

        if (reason.Length > MaxReasonLength)
            return ValidationProblem($"Reason must not exceed {MaxReasonLength} characters.", "REASON_TOO_LONG");

        if (SensitiveReasonPattern.IsMatch(reason))
            return ValidationProblem("Reason must not contain sensitive terms (password, token, secret, hash, security_stamp, or key material).", "REASON_CONTAINS_SENSITIVE_TERM");

        return null;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private IActionResult MapResult(AccountManagementResult result)
    {
        if (result.Succeeded) return NoContent();
        return MapErrorResult(result);
    }

    private IActionResult MapErrorResult(AccountManagementResult result)
    {
        return result.ErrorCode switch
        {
            "AUTH_ACCOUNT_NOT_FOUND" =>
                NotFoundProblem("AUTH_ACCOUNT_NOT_FOUND", "Account not found."),

            "AUTH_ACCOUNT_STATE_CONFLICT" =>
                ConflictProblem("AUTH_ACCOUNT_STATE_CONFLICT", "The account state transition is not allowed."),

            "AUTH_EXTERNAL_PASSWORD_MANAGED" =>
                ConflictProblem("AUTH_EXTERNAL_PASSWORD_MANAGED", "Password for this account is managed by an external provider."),

            "AUTH_PASSWORD_REUSE" =>
                UnprocessableProblem("AUTH_PASSWORD_REUSE", "The temporary password matches a recently used password."),

            "AUTH_PASSWORD_LENGTH_INVALID" =>
                UnprocessableProblem("AUTH_PASSWORD_LENGTH_INVALID", "The generated password does not meet length requirements."),

            "AUTH_PASSWORD_CONTAINS_PROVIDER_SUBJECT" =>
                UnprocessableProblem("AUTH_PASSWORD_CONTAINS_PROVIDER_SUBJECT", "The generated password contains a disallowed pattern."),

            "AUTH_ACCOUNT_CONCURRENCY_CONFLICT" =>
                ConflictProblem("AUTH_ACCOUNT_CONCURRENCY_CONFLICT", "The account was modified concurrently."),

            _ =>
                StatusCode(StatusCodes.Status500InternalServerError, BuildProblem(
                    StatusCodes.Status500InternalServerError,
                    "ACCOUNT_OPERATION_FAILED",
                    "The operation could not be completed."))
        };
    }

    private IActionResult ValidationProblem(string detail, string errorCode) =>
        BadRequest(BuildProblem(StatusCodes.Status400BadRequest, errorCode, detail));

    private IActionResult NotFoundProblem(string errorCode, string detail) =>
        NotFound(BuildProblem(StatusCodes.Status404NotFound, errorCode, detail));

    private IActionResult ConflictProblem(string errorCode, string detail) =>
        Conflict(BuildProblem(StatusCodes.Status409Conflict, errorCode, detail));

    private IActionResult UnprocessableProblem(string errorCode, string detail) =>
        StatusCode(StatusCodes.Status422UnprocessableEntity,
            BuildProblem(StatusCodes.Status422UnprocessableEntity, errorCode, detail));

    private static ProblemDetails BuildProblem(int status, string errorCode, string detail)
    {
        var p = new ProblemDetails
        {
            Status = status,
            Title = "Account Management Error",
            Detail = detail,
            Type = "https://ptkd-erp.internal/docs/errors/account-management"
        };
        p.Extensions["errorCode"] = errorCode;
        return p;
    }
}

public sealed record AccountReasonRequest(string Reason);
public sealed record AdminResetPasswordDto(string TemporaryPassword);
