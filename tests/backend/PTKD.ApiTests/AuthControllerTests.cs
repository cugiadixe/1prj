using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Api.Auth.Models;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Security.Authentication;
using PTKD.IntegrationTests;

namespace PTKD.ApiTests;

[Collection("Sequential")]
public class AuthControllerTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── Login tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccessAndSetsCookies()
    {
        // 1. Arrange - Seed data in PTKD_TEST_PHASE1A2
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        var accountId = await SeedUserAndAccountAsync(testUsername, testPassword);

        var request = new LoginRequest(testUsername, testPassword);

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/v2/auth/login", request);

        // 3. Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.AccessToken);
        Assert.Equal("Bearer", loginResponse.TokenType);
        Assert.Equal(900, loginResponse.ExpiresIn);
        Assert.Equal(testUsername, loginResponse.User.Username);

        // Verify Refresh Cookie
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();

        // Ensure no __Host- cookies are returned
        Assert.DoesNotContain(setCookieHeaders, c => c.StartsWith("__Host-", StringComparison.OrdinalIgnoreCase));

        var refreshCookie = setCookieHeaders.SingleOrDefault(c => c.StartsWith("RefreshToken="));
        Assert.NotNull(refreshCookie);
        Assert.Contains("HttpOnly", refreshCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Secure", refreshCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SameSite=Strict", refreshCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Path=/api/v2/auth", refreshCookie, StringComparison.OrdinalIgnoreCase);

        // Verify CSRF Cookie & Header
        var csrfCookie = setCookieHeaders.SingleOrDefault(c => c.StartsWith("X-CSRF-TOKEN="));
        Assert.NotNull(csrfCookie);
        Assert.DoesNotContain("HttpOnly", csrfCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Secure", csrfCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Path=/", csrfCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Path=/api/v2/auth", csrfCookie, StringComparison.OrdinalIgnoreCase);

        var csrfHeader = response.Headers.GetValues("X-CSRF-Token").FirstOrDefault();
        Assert.NotNull(csrfHeader);
    }

    /// <summary>
    /// Test 1: Locked account with correct password must map to HTTP 403 generic.
    /// </summary>
    [Fact]
    public async Task Login_LockedAccount_Returns403Generic()
    {
        var testUsername = "api_locked_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword, locked: true);

        var response = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = problem.GetProperty("type").GetString() ?? string.Empty;
        var detail = problem.GetProperty("detail").GetString() ?? string.Empty;

        Assert.Equal("https://ptkd-erp.internal/docs/errors/auth/access-denied", type);
        Assert.DoesNotContain("LOCKED", detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lock", detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ACCOUNT_LOCKED", detail, StringComparison.OrdinalIgnoreCase);

        // Assert no access token / cookies
        var rawJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("accessToken", rawJson, StringComparison.OrdinalIgnoreCase);

        bool hasSetCookie = response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders);
        if (hasSetCookie && setCookieHeaders != null)
        {
            Assert.Empty(setCookieHeaders);
        }
    }

    /// <summary>
    /// Test 2: Locked account with WRONG password must map to HTTP 401 generic,
    /// so as not to enumerate password correctness to unauthenticated users.
    /// </summary>
    [Fact]
    public async Task Login_LockedAccount_WrongPassword_Returns401Generic()
    {
        var testUsername = "api_locked_wp_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword, locked: true);

        var response = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var type = problem.GetProperty("type").GetString() ?? string.Empty;
        Assert.Equal("https://ptkd-erp.internal/docs/errors/auth/invalid-credentials", type);
    }

    /// <summary>
    /// Test 3: Unknown account must return HTTP 401 generic.
    /// </summary>
    [Fact]
    public async Task Login_UnknownAccount_Returns401Generic()
    {
        var request = new LoginRequest("nonexistent", "wrongpass");
        var response = await _client.PostAsJsonAsync("/api/v2/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://ptkd-erp.internal/docs/errors/auth/invalid-credentials", problem.GetProperty("type").GetString());
    }

    /// <summary>
    /// Test 4: Invalid password for non-locked account must return HTTP 401 generic.
    /// </summary>
    [Fact]
    public async Task Login_InvalidPassword_Returns401Generic()
    {
        var testUsername = "api_active_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword, locked: false);

        var response = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://ptkd-erp.internal/docs/errors/auth/invalid-credentials", problem.GetProperty("type").GetString());
    }

    /// <summary>
    /// Test B1#2: Login success response body does NOT contain refresh token material.
    /// Asserts that no known refresh token field name appears in the raw JSON body.
    /// </summary>
    [Fact]
    public async Task Login_Success_ResponseBodyDoesNotContainRefreshToken()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var response = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();

        // Assert no refresh token field name appears in the JSON body
        Assert.DoesNotContain("refreshToken", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh_token", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rawRefreshToken", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshMaterial", rawJson, StringComparison.OrdinalIgnoreCase);

        // Also assert expected fields ARE present
        Assert.Contains("accessToken", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    // ── Refresh tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidCookies_ReturnsNewToken()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var cookies = loginRes.Headers.GetValues("Set-Cookie");
        var csrfToken = loginRes.Headers.GetValues("X-CSRF-Token").First();

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/refresh");
        refreshRequest.Headers.Add("Cookie", cookies);
        refreshRequest.Headers.Add("X-CSRF-Token", csrfToken);

        var refreshRes = await _client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.OK, refreshRes.StatusCode);
        var newLoginRes = await refreshRes.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(newLoginRes?.AccessToken);

        var newCookies = refreshRes.Headers.GetValues("Set-Cookie").ToList();

        // Ensure no __Host- cookies are returned
        Assert.DoesNotContain(newCookies, c => c.StartsWith("__Host-", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(newCookies, c => c.StartsWith("RefreshToken="));
        var newCsrfCookie = newCookies.SingleOrDefault(c => c.StartsWith("X-CSRF-TOKEN="));
        Assert.NotNull(newCsrfCookie);
        Assert.DoesNotContain("HttpOnly", newCsrfCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Path=/", newCsrfCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Path=/api/v2/auth", newCsrfCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_MissingCsrf_Returns403()
    {
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/refresh");
        refreshRequest.Headers.Add("Cookie", "RefreshToken=fake-token; X-CSRF-TOKEN=fake-csrf");
        // No header X-CSRF-Token

        var refreshRes = await _client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.Forbidden, refreshRes.StatusCode);
    }

    /// <summary>
    /// Test B1#3: Missing refresh cookie (with valid CSRF) maps to generic 401.
    /// </summary>
    [Fact]
    public async Task Refresh_MissingRefreshCookie_Returns401Generic()
    {
        // Supply CSRF cookie and header, but no RefreshToken cookie
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/refresh");
        var fakeToken = "fake-csrf-value";
        refreshRequest.Headers.Add("Cookie", $"X-CSRF-TOKEN={fakeToken}");
        refreshRequest.Headers.Add("X-CSRF-Token", fakeToken);

        var refreshRes = await _client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);

        var problem = await refreshRes.Content.ReadFromJsonAsync<JsonElement>();
        var type = problem.GetProperty("type").GetString() ?? string.Empty;
        // Must not expose internal token state
        Assert.DoesNotContain("not_found", type, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("missing", type, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test B1#4: Invalid refresh token maps to generic 401 response.
    /// </summary>
    [Fact]
    public async Task Refresh_InvalidToken_Returns401Generic()
    {
        var fakeToken = "invalid-csrf-value";
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"RefreshToken=this-is-not-a-real-token; X-CSRF-TOKEN={fakeToken}");
        refreshRequest.Headers.Add("X-CSRF-Token", fakeToken);

        var refreshRes = await _client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);

        var problem = await refreshRes.Content.ReadFromJsonAsync<JsonElement>();
        var type = problem.GetProperty("type").GetString() ?? string.Empty;
        Assert.Equal("https://ptkd-erp.internal/docs/errors/auth/session-invalid", type);
        // Must not expose internal reason codes
        var detail = problem.GetProperty("detail").GetString() ?? string.Empty;
        Assert.DoesNotContain("TOKEN_NOT_FOUND", detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InternalReason", detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test B1#5+6: Revoked/expired refresh token maps to generic 401 response.
    /// We simulate a revoked token by logging out after login (which revokes the family),
    /// then trying to refresh with the original token.
    /// This covers both revocation and post-logout invalid state.
    /// </summary>
    [Fact]
    public async Task Refresh_RevokedToken_Returns401Generic()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        // Login to get a real refresh token
        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        var cookies = loginRes.Headers.GetValues("Set-Cookie").ToList();
        var csrfToken = loginRes.Headers.GetValues("X-CSRF-Token").First();

        // Logout to revoke the token family
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/logout");
        logoutReq.Headers.Add("Cookie", cookies);
        logoutReq.Headers.Add("X-CSRF-Token", csrfToken);
        var logoutRes = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.NoContent, logoutRes.StatusCode);

        // Now try to refresh with the revoked token
        var newCsrfToken = "new-fake-csrf";
        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/refresh");
        // Use the original refresh token cookie but a new (matching) CSRF
        var refreshTokenCookie = cookies.First(c => c.StartsWith("RefreshToken="));
        var refreshTokenValue = refreshTokenCookie.Split(';')[0]; // "RefreshToken=<value>"
        refreshReq.Headers.Add("Cookie", $"{refreshTokenValue}; X-CSRF-TOKEN={newCsrfToken}");
        refreshReq.Headers.Add("X-CSRF-Token", newCsrfToken);

        var refreshRes = await _client.SendAsync(refreshReq);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);

        var problem = await refreshRes.Content.ReadFromJsonAsync<JsonElement>();
        var type = problem.GetProperty("type").GetString() ?? string.Empty;
        Assert.Equal("https://ptkd-erp.internal/docs/errors/auth/session-invalid", type);
        // Internal revoke reason must not be exposed
        var detail = problem.GetProperty("detail").GetString() ?? string.Empty;
        Assert.DoesNotContain("TOKEN_REVOKED", detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SESSIONS_INVALIDATED", detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test B1#7: Reused refresh token (token rotation reuse detection) maps to generic 401 response.
    /// After a successful refresh, the old token is marked used; reusing it should fail.
    /// </summary>
    [Fact]
    public async Task Refresh_ReusedToken_Returns401Generic()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        // Login
        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        var loginCookies = loginRes.Headers.GetValues("Set-Cookie").ToList();
        var loginCsrfToken = loginRes.Headers.GetValues("X-CSRF-Token").First();

        // First refresh (legitimate rotation)
        var firstRefreshToken = loginCookies.First(c => c.StartsWith("RefreshToken=")).Split(';')[0];
        var firstRefreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/refresh");
        firstRefreshReq.Headers.Add("Cookie", loginCookies);
        firstRefreshReq.Headers.Add("X-CSRF-Token", loginCsrfToken);
        var firstRefreshRes = await _client.SendAsync(firstRefreshReq);
        Assert.Equal(HttpStatusCode.OK, firstRefreshRes.StatusCode);

        // Now attempt to reuse the ORIGINAL (now-used/rotated) refresh token
        var newCsrfToken = "reuse-test-csrf";
        var reuseReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/refresh");
        reuseReq.Headers.Add("Cookie", $"{firstRefreshToken}; X-CSRF-TOKEN={newCsrfToken}");
        reuseReq.Headers.Add("X-CSRF-Token", newCsrfToken);
        var reuseRes = await _client.SendAsync(reuseReq);

        Assert.Equal(HttpStatusCode.Unauthorized, reuseRes.StatusCode);

        var problem = await reuseRes.Content.ReadFromJsonAsync<JsonElement>();
        var type = problem.GetProperty("type").GetString() ?? string.Empty;
        Assert.Equal("https://ptkd-erp.internal/docs/errors/auth/session-invalid", type);
        // Must not expose TOKEN_REUSED internal code
        var detail = problem.GetProperty("detail").GetString() ?? string.Empty;
        Assert.DoesNotContain("TOKEN_REUSED", detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Test B1#8: Refresh success response body does NOT contain refresh token material.
    /// </summary>
    [Fact]
    public async Task Refresh_Success_ResponseBodyDoesNotContainRefreshToken()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var cookies = loginRes.Headers.GetValues("Set-Cookie");
        var csrfToken = loginRes.Headers.GetValues("X-CSRF-Token").First();

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/refresh");
        refreshRequest.Headers.Add("Cookie", cookies);
        refreshRequest.Headers.Add("X-CSRF-Token", csrfToken);

        var refreshRes = await _client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshRes.StatusCode);

        var rawJson = await refreshRes.Content.ReadAsStringAsync();

        Assert.DoesNotContain("refreshToken", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh_token", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rawRefreshToken", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshMaterial", rawJson, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("accessToken", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    // ── Logout tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ValidSession_Returns204AndClearsCookies()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var cookies = loginRes.Headers.GetValues("Set-Cookie");
        var csrfToken = loginRes.Headers.GetValues("X-CSRF-Token").First();

        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/logout");
        logoutReq.Headers.Add("Cookie", cookies);
        logoutReq.Headers.Add("X-CSRF-Token", csrfToken);

        var logoutRes = await _client.SendAsync(logoutReq);

        Assert.Equal(HttpStatusCode.NoContent, logoutRes.StatusCode);

        var clearCookies = logoutRes.Headers.GetValues("Set-Cookie").ToList();

        // Ensure no __Host- cookies are returned
        Assert.DoesNotContain(clearCookies, c => c.StartsWith("__Host-", StringComparison.OrdinalIgnoreCase));

        // Cookie clearing involves setting it to empty with past expiry
        Assert.Contains(clearCookies, c => c.StartsWith("RefreshToken=") && c.Contains("expires="));
    }

    /// <summary>
    /// Test B1#9: Logout requires CSRF when refresh cookie is present.
    /// Missing CSRF header must fail with 403.
    /// </summary>
    [Fact]
    public async Task Logout_MissingCsrf_WithRefreshCookiePresent_Returns403()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var cookies = loginRes.Headers.GetValues("Set-Cookie");

        // Send logout with RefreshToken cookie but NO X-CSRF-Token header
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/logout");
        logoutReq.Headers.Add("Cookie", cookies); // has RefreshToken + X-CSRF-TOKEN cookies
        // No X-CSRF-Token header

        var logoutRes = await _client.SendAsync(logoutReq);

        Assert.Equal(HttpStatusCode.Forbidden, logoutRes.StatusCode);
    }

    /// <summary>
    /// Test B1#10: Logout revokes current family/session.
    /// After logout, a refresh with the same token must return 401.
    /// </summary>
    [Fact]
    public async Task Logout_RevokesSession_SubsequentRefreshFails()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        // Login
        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var cookies = loginRes.Headers.GetValues("Set-Cookie").ToList();
        var csrfToken = loginRes.Headers.GetValues("X-CSRF-Token").First();

        // Logout
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/logout");
        logoutReq.Headers.Add("Cookie", cookies);
        logoutReq.Headers.Add("X-CSRF-Token", csrfToken);
        var logoutRes = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.NoContent, logoutRes.StatusCode);

        // Try to refresh after logout — must fail
        var newCsrfToken = "post-logout-csrf";
        var refreshTokenCookie = cookies.First(c => c.StartsWith("RefreshToken=")).Split(';')[0];
        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/refresh");
        refreshReq.Headers.Add("Cookie", $"{refreshTokenCookie}; X-CSRF-TOKEN={newCsrfToken}");
        refreshReq.Headers.Add("X-CSRF-Token", newCsrfToken);

        var refreshRes = await _client.SendAsync(refreshReq);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);
    }

    /// <summary>
    /// Test B1#11+12: Logout deletes both RefreshToken and X-CSRF-TOKEN cookies.
    /// </summary>
    [Fact]
    public async Task Logout_DeletesRefreshCookieAndCsrfCookie()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var cookies = loginRes.Headers.GetValues("Set-Cookie");
        var csrfToken = loginRes.Headers.GetValues("X-CSRF-Token").First();

        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/logout");
        logoutReq.Headers.Add("Cookie", cookies);
        logoutReq.Headers.Add("X-CSRF-Token", csrfToken);
        var logoutRes = await _client.SendAsync(logoutReq);

        Assert.Equal(HttpStatusCode.NoContent, logoutRes.StatusCode);

        var clearCookies = logoutRes.Headers.GetValues("Set-Cookie").ToList();

        // RefreshToken cookie must be cleared (expired)
        Assert.Contains(clearCookies, c => c.StartsWith("RefreshToken=") && c.Contains("expires="));

        // X-CSRF-TOKEN cookie must be cleared (expired or empty)
        Assert.Contains(clearCookies, c => c.StartsWith("X-CSRF-TOKEN=") && c.Contains("expires="));
    }

    /// <summary>
    /// Test B1#13: Logout with no refresh cookie returns safe 204 (no CSRF required).
    /// </summary>
    [Fact]
    public async Task Logout_NoCookiePresent_Returns204Safe()
    {
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/logout");
        // No cookies, no CSRF header — safe to ignore

        var logoutRes = await _client.SendAsync(logoutReq);

        // When no refresh cookie is present, logout succeeds generically (204)
        Assert.Equal(HttpStatusCode.NoContent, logoutRes.StatusCode);
    }

    // ── Change Password tests ─────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidCsrf_ChangesPasswordAndClearsCookies()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var cookies = loginRes.Headers.GetValues("Set-Cookie");
        var csrfToken = loginRes.Headers.GetValues("X-CSRF-Token").First();
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        var changePwdReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/change-password");
        changePwdReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        changePwdReq.Headers.Add("Cookie", cookies);
        changePwdReq.Headers.Add("X-CSRF-Token", csrfToken);
        changePwdReq.Content = JsonContent.Create(new PTKD.Api.Auth.Models.ChangePasswordRequest(testPassword, "NewValidPassword123!"));

        var res = await _client.SendAsync(changePwdReq);

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        // Check if cookies are cleared (to force re-login)
        var clearCookies = res.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(clearCookies, c => c.StartsWith("RefreshToken=") && c.Contains("expires="));
        Assert.Contains(clearCookies, c => c.StartsWith("X-CSRF-TOKEN=") && c.Contains("expires="));
    }

    [Fact]
    public async Task ChangePassword_MissingCsrf_Returns403()
    {
        var testUsername = "api_test_user_" + Guid.NewGuid().ToString("N")[..8];
        var testPassword = "ValidPassword123!";
        await SeedUserAndAccountAsync(testUsername, testPassword);

        var loginRes = await _client.PostAsJsonAsync("/api/v2/auth/login", new LoginRequest(testUsername, testPassword));
        var cookies = loginRes.Headers.GetValues("Set-Cookie");
        var loginResponse = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginResponse!.AccessToken;

        var changePwdReq = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/change-password");
        changePwdReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        changePwdReq.Headers.Add("Cookie", cookies);
        // NO CSRF HEADER
        changePwdReq.Content = JsonContent.Create(new PTKD.Api.Auth.Models.ChangePasswordRequest(testPassword, "NewValidPassword123!"));

        var res = await _client.SendAsync(changePwdReq);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ── Scope tests ───────────────────────────────────────────────────────────

    /// <summary>
    /// Test B1#14: GET /api/v2/auth/me is not implemented (404 or MethodNotAllowed).
    /// </summary>
    [Fact]
    public async Task Auth_Me_Endpoint_NotPresent()
    {
        var response = await _client.GetAsync("/api/v2/auth/me");

        // Must return 404 (not found) — endpoint not routed
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test B1#15: POST /api/v2/auth/logout-all is not implemented (404).
    /// </summary>
    [Fact]
    public async Task Auth_LogoutAll_Endpoint_NotPresent()
    {
        var response = await _client.PostAsJsonAsync("/api/v2/auth/logout-all", new { });

        // Must return 404 (not found) — endpoint not routed
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private async Task<long> SeedUserAndAccountAsync(
        string username,
        string password,
        bool locked = false)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<PTKD.Application.Common.Interfaces.IOrganizationDbContextFactory>();
        var authContextFactory = scope.ServiceProvider.GetRequiredService<IAuthenticationDbContextFactory>();
        var hashService = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
        var clock = scope.ServiceProvider.GetRequiredService<IUtcClock>();

        using var db = (PTKD.Infrastructure.Persistence.AppDbContext)dbContextFactory.CreateDbContext();

        var user = new PTKD.Domain.Entities.User(
            username,
            "Test " + username,
            username + "@test.internal",
            "ACTIVE",
            "ACTIVE"
        );
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hasher = new PasswordHasher<PTKD.Domain.Entities.UserAuthAccount>();
        var hash = hasher.HashPassword(null!, password);

        var account = PTKD.Domain.Entities.UserAuthAccount.CreateInternal(
            user.Id,
            username.ToUpperInvariant(),
            hash,
            clock.UtcNow
        );

        if (locked)
        {
            // Record enough failed attempts to trigger lockout (policy default: 5 attempts, 15 min lockout)
            var policy = new AuthenticationAccountPolicy();
            for (var i = 0; i < policy.MaximumFailedAttempts; i++)
            {
                account.RecordFailedAttempt(clock.UtcNow, policy.MaximumFailedAttempts, policy.LockoutDuration);
            }
        }

        db.UserAuthAccounts.Add(account);
        await db.SaveChangesAsync();

        return account.Id;
    }
}
