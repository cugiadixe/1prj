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

        var csrfHeader = response.Headers.GetValues("X-CSRF-Token").FirstOrDefault();
        Assert.NotNull(csrfHeader);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401Generic()
    {
        var request = new LoginRequest("nonexistent", "wrongpass");
        var response = await _client.PostAsJsonAsync("/api/v2/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://ptkd-erp.internal/docs/errors/auth/invalid-credentials", problem.GetProperty("type").GetString());
    }

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
        Assert.Contains(newCookies, c => c.StartsWith("X-CSRF-TOKEN="));
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

    private async Task<long> SeedUserAndAccountAsync(string username, string password)
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
        db.UserAuthAccounts.Add(account);
        await db.SaveChangesAsync();

        return account.Id;
    }
}
