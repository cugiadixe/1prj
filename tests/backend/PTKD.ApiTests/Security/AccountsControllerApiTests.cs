using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Application.Security.AccountManagement.DTOs;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;
using PTKD.Infrastructure.Persistence;

namespace PTKD.ApiTests.Security;

[Collection("Sequential")]
public sealed class AccountsControllerApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public AccountsControllerApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAccountDetail_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v2/security/accounts/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountDetail_WithoutPermission_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v2/security/accounts/1");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountDetail_NotFound_Returns404()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync("/api/v2/security/accounts/999999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("AUTH_ACCOUNT_NOT_FOUND", problem.Extensions["errorCode"]?.ToString());
    }

    // ── View account detail ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAccountDetail_WithPermission_ReturnsSafeFields()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.UserAuthAccounts.FirstAsync(a => a.UserId == userId);

        var response = await client.GetAsync($"/api/v2/security/accounts/{account.Id}");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<AccountDetailDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(dto);
        Assert.Equal(account.Id, dto.Id);
        Assert.Equal(userId, dto.UserId);
        Assert.Equal("INTERNAL", dto.ProviderType);
        Assert.Equal("ACTIVE", dto.Status);
    }

    [Fact]
    public async Task GetAccountDetail_ResponseJson_DoesNotExposePasswordHashOrSecurityStamp()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.UserAuthAccounts.FirstAsync(a => a.UserId == userId);

        var response = await client.GetAsync($"/api/v2/security/accounts/{account.Id}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password_hash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("security_stamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rowVersion", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionsInvalidatedAt", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Activate_DisabledAccount_Returns204()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var targetAccountId = await CreateDisabledAccountAsync(userId);

        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/activate", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.UserAuthAccounts.FirstAsync(a => a.Id == targetAccountId);
        Assert.Equal("ACTIVE", updated.AuthAccountStatus);
    }

    [Fact]
    public async Task Activate_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/v2/security/accounts/1/activate", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Activate_WritesAuditEvent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateDisabledAccountAsync(userId);

        await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/activate", null);

        var auditRow = FindAuditEvent("ACCOUNT_ACTIVATED", targetAccountId);
        Assert.NotNull(auditRow);
        Assert.Equal("SUCCESS", auditRow.Value.Outcome);
    }

    // ── Disable ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Disable_WithReason_Returns204()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateActiveAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Policy violation" });
        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/disable", body);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.UserAuthAccounts.FirstAsync(a => a.Id == targetAccountId);
        Assert.Equal("DISABLED", updated.AuthAccountStatus);
    }

    [Fact]
    public async Task Disable_WithoutReason_Returns400()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var body = JsonContent.Create(new { Reason = "" });
        var response = await client.PostAsync("/api/v2/security/accounts/1/disable", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("REASON_REQUIRED", problem.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Disable_AlreadyDisabled_Returns409()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateDisabledAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Test" });
        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/disable", body);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Disable_WritesAuditEvent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateActiveAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Audit test" });
        await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/disable", body);

        var auditRow = FindAuditEvent("ACCOUNT_DISABLED", targetAccountId);
        Assert.NotNull(auditRow);
        Assert.Equal("SUCCESS", auditRow.Value.Outcome);
        Assert.Equal("Audit test", auditRow.Value.Reason);
    }

    // ── Lock ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Lock_WithReason_Returns204()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateActiveAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Suspicious activity" });
        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/lock", body);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.UserAuthAccounts.FirstAsync(a => a.Id == targetAccountId);
        Assert.Equal("LOCKED", updated.AuthAccountStatus);
        Assert.Null(updated.LockoutEnd);
    }

    [Fact]
    public async Task Lock_WithoutReason_Returns400()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var body = JsonContent.Create(new { Reason = "  " });
        var response = await client.PostAsync("/api/v2/security/accounts/1/lock", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Lock_DisabledAccount_Returns409()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateDisabledAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Test" });
        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/lock", body);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Lock_WritesAuditEvent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateActiveAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Lock audit test" });
        await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/lock", body);

        var auditRow = FindAuditEvent("ACCOUNT_LOCKED", targetAccountId);
        Assert.NotNull(auditRow);
        Assert.Equal("Lock audit test", auditRow.Value.Reason);
    }

    // ── Unlock ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unlock_LockedAccount_Returns204()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateLockedAccountAsync(userId);

        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/unlock", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.UserAuthAccounts.FirstAsync(a => a.Id == targetAccountId);
        Assert.Equal("ACTIVE", updated.AuthAccountStatus);
    }

    [Fact]
    public async Task Unlock_DisabledAccount_Returns409()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateDisabledAccountAsync(userId);

        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/unlock", null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Unlock_DoesNotResetPassword()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateLockedAccountAsync(userId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await db.UserAuthAccounts.AsNoTracking().FirstAsync(a => a.Id == targetAccountId);

        await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/unlock", null);

        var after = await db.UserAuthAccounts.AsNoTracking().FirstAsync(a => a.Id == targetAccountId);
        Assert.Equal(before.PasswordHash, after.PasswordHash);
        Assert.Equal(before.MustChangePassword, after.MustChangePassword);
    }

    [Fact]
    public async Task Unlock_WritesAuditEvent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateLockedAccountAsync(userId);

        await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/unlock", null);

        var auditRow = FindAuditEvent("ACCOUNT_UNLOCKED", targetAccountId);
        Assert.NotNull(auditRow);
    }

    // ── Admin password reset ──────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_WithReason_Returns200AndTemporaryPassword()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateInternalAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Admin reset" });
        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/reset-password", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("temporaryPassword", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"temporaryPassword\":\"\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetPassword_WithoutReason_Returns400()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var body = JsonContent.Create(new { Reason = "" });
        var response = await client.PostAsync("/api/v2/security/accounts/1/reset-password", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_SetsMustChangePassword()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateInternalAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Force change" });
        await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/reset-password", body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.UserAuthAccounts.FirstAsync(a => a.Id == targetAccountId);
        Assert.True(updated.MustChangePassword);
    }

    [Fact]
    public async Task ResetPassword_ResponseJson_DoesNotContainPasswordHash()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateInternalAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Redaction check" });
        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/reset-password", body);
        var json = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetPassword_AuditEventDoesNotContainPasswordMaterial()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateInternalAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Audit check" });
        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/reset-password", body);

        var dto = await response.Content.ReadFromJsonAsync<JsonElement>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var temporaryPassword = dto.GetProperty("temporaryPassword").GetString();
        Assert.NotNull(temporaryPassword);

        var auditRow = FindAuditEvent("ACCOUNT_PASSWORD_RESET_BY_ADMIN", targetAccountId);
        Assert.NotNull(auditRow);

        if (auditRow.Value.Reason is not null)
            Assert.DoesNotContain(temporaryPassword, auditRow.Value.Reason, StringComparison.Ordinal);
    }

    // ── Revoke sessions ───────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeSessions_WithReason_Returns204()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateActiveAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Security incident" });
        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/revoke-sessions", body);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RevokeSessions_WithoutReason_Returns400()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var body = JsonContent.Create(new { Reason = "" });
        var response = await client.PostAsync("/api/v2/security/accounts/1/revoke-sessions", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RevokeSessions_DoesNotChangePassword()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateInternalAccountAsync(userId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var before = await db.UserAuthAccounts.AsNoTracking().FirstAsync(a => a.Id == targetAccountId);

        var body = JsonContent.Create(new { Reason = "Revoke check" });
        await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/revoke-sessions", body);

        var after = await db.UserAuthAccounts.AsNoTracking().FirstAsync(a => a.Id == targetAccountId);
        Assert.Equal(before.PasswordHash, after.PasswordHash);
        Assert.Equal(before.AuthAccountStatus, after.AuthAccountStatus);
    }

    [Fact]
    public async Task RevokeSessions_DoesNotChangeAccountStatus()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateActiveAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Status check" });
        await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/revoke-sessions", body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.UserAuthAccounts.FirstAsync(a => a.Id == targetAccountId);
        Assert.Equal("ACTIVE", updated.AuthAccountStatus);
    }

    [Fact]
    public async Task RevokeSessions_WritesAuditEvent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateActiveAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "Session revoke audit" });
        await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/revoke-sessions", body);

        var auditRow = FindAuditEvent("ACCOUNT_SESSIONS_REVOKED", targetAccountId);
        Assert.NotNull(auditRow);
        Assert.Equal("Session revoke audit", auditRow.Value.Reason);
    }

    // ── Reason safety ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("password")]
    [InlineData("Contains token value")]
    [InlineData("The secret is here")]
    [InlineData("Changed hash")]
    [InlineData("Reset SecurityStamp")]
    [InlineData("security_stamp bumped")]
    [InlineData("temp password issued")]
    [InlineData("temporaryPassword was sent")]
    [InlineData("signing_key rotated")]
    [InlineData("api_key exposed")]
    [InlineData("access_key leaked")]
    public async Task Disable_WithSensitiveReason_Returns400(string sensitiveReason)
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var body = JsonContent.Create(new { Reason = sensitiveReason });
        var response = await client.PostAsync("/api/v2/security/accounts/1/disable", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("REASON_CONTAINS_SENSITIVE_TERM", problem.Extensions["errorCode"]?.ToString());
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Includes token data")]
    public async Task Lock_WithSensitiveReason_Returns400(string sensitiveReason)
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var body = JsonContent.Create(new { Reason = sensitiveReason });
        var response = await client.PostAsync("/api/v2/security/accounts/1/lock", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("REASON_CONTAINS_SENSITIVE_TERM", problem.Extensions["errorCode"]?.ToString());
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Contains secret")]
    public async Task ResetPassword_WithSensitiveReason_Returns400(string sensitiveReason)
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var body = JsonContent.Create(new { Reason = sensitiveReason });
        var response = await client.PostAsync("/api/v2/security/accounts/1/reset-password", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("REASON_CONTAINS_SENSITIVE_TERM", problem.Extensions["errorCode"]?.ToString());
    }

    [Theory]
    [InlineData("password")]
    [InlineData("refresh token stolen")]
    public async Task RevokeSessions_WithSensitiveReason_Returns400(string sensitiveReason)
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var body = JsonContent.Create(new { Reason = sensitiveReason });
        var response = await client.PostAsync("/api/v2/security/accounts/1/revoke-sessions", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("REASON_CONTAINS_SENSITIVE_TERM", problem.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Disable_WithReasonExceedingMaxLength_Returns400()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var body = JsonContent.Create(new { Reason = new string('A', 501) });
        var response = await client.PostAsync("/api/v2/security/accounts/1/disable", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("REASON_TOO_LONG", problem.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Disable_WithValidSafeReason_Accepted()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");
        var targetAccountId = await CreateActiveAccountAsync(userId);

        var body = JsonContent.Create(new { Reason = "User requested account deactivation per HR policy" });
        var response = await client.PostAsync($"/api/v2/security/accounts/{targetAccountId}/disable", body);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Security / data exposure ──────────────────────────────────────────────

    [Fact]
    public async Task NoErrorResponse_ExposesStackTraceOrSqlText()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ACCOUNT_MANAGE");

        var response = await client.GetAsync("/api/v2/security/accounts/999999999");
        var json = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("StackTrace", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SqlException", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<long> CreateActiveAccountAsync(long callerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var uid = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"tgt_{uid}", "Target User", $"target_{uid}@ptkd.local", "ACTIVE", "ACTIVE");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var account = UserAuthAccount.CreateInternal(user.Id, $"tgt_{uid}", "hash_active", now);
        db.UserAuthAccounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    private async Task<long> CreateDisabledAccountAsync(long callerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var uid = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"dis_{uid}", "Disabled User", $"disabled_{uid}@ptkd.local", "ACTIVE", "ACTIVE");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var account = UserAuthAccount.CreateInternal(user.Id, $"dis_{uid}", "hash_disabled", now);
        account.Disable(now, callerUserId);
        db.UserAuthAccounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    private async Task<long> CreateLockedAccountAsync(long callerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var uid = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"lck_{uid}", "Locked User", $"locked_{uid}@ptkd.local", "ACTIVE", "ACTIVE");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var account = UserAuthAccount.CreateInternal(user.Id, $"lck_{uid}", "hash_locked", now);
        account.Lock(now, callerUserId);
        db.UserAuthAccounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    private async Task<long> CreateInternalAccountAsync(long callerUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
        var now = DateTime.UtcNow;
        var uid = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"int_{uid}", "Internal User", $"internal_{uid}@ptkd.local", "ACTIVE", "ACTIVE");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var provisional = UserAuthAccount.CreateInternal(user.Id, $"int_{uid}", "placeholder", now);
        var realHash = passwordHasher.HashPassword(provisional, "Initial@Password123!");
        var account = UserAuthAccount.CreateInternal(user.Id, $"int_{uid}", realHash, now);
        db.UserAuthAccounts.Add(account);
        await db.SaveChangesAsync();
        return account.Id;
    }

    private (string EventCode, string Outcome, string? Reason)? FindAuditEvent(string eventCode, long entityId)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connStr = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not configured.");

        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = new SqlCommand(
            """
            SELECT TOP 1 event_code, outcome, reason
            FROM dbo.Security_Audit_Events
            WHERE event_code = @eventCode AND entity_id = @entityId
            ORDER BY id DESC;
            """, conn);
        cmd.Parameters.AddWithValue("@eventCode", eventCode);
        cmd.Parameters.AddWithValue("@entityId", entityId.ToString());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return (
            EventCode: reader.GetString(0),
            Outcome: reader.GetString(1),
            Reason: reader.IsDBNull(2) ? null : reader.GetString(2));
    }
}
