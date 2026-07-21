using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;
using PTKD.Infrastructure.Persistence;
using Xunit;

namespace PTKD.ApiTests;

[Collection("Sequential")]
public class ProtectedEndpointIntegrationTests : IClassFixture<SafeTestWebApplicationFactory>, IAsyncLifetime
{
    private readonly SafeTestWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private IServiceScope _scope = null!;
    private AppDbContext _dbContext = null!;
    private IJwtAccessTokenService _jwtService = null!;

    public ProtectedEndpointIntegrationTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _jwtService = _scope.ServiceProvider.GetRequiredService<IJwtAccessTokenService>();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        _client?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<UserAuthAccount> CreateActiveUserAndAccountAsync()
    {
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        var user = new User($"emp_{unique}", "Test User", $"test_{unique}@ptkd.local", "ACTIVE", "ACTIVE");
        
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, $"sub_{unique}", "hash", DateTime.UtcNow);
        _dbContext.UserAuthAccounts.Add(account);
        await _dbContext.SaveChangesAsync();

        return account;
    }

    private string GenerateToken(UserAuthAccount account, TimeSpan offset = default)
    {
        var request = new AccessTokenRequest(
            account.UserId,
            account.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            account.SecurityStamp,
            "admin",
            account.MustChangePassword);
            
        var result = _jwtService.IssueAccessToken(request);
        return result.Token;
    }

    [Fact]
    public async Task ProtectedEndpoint_ValidToken_Succeeds()
    {
        var account = await CreateActiveUserAndAccountAsync();
        var token = GenerateToken(account, TimeSpan.FromMinutes(15));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/test/ProtectedTest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_MissingToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v2/test/ProtectedTest");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_InvalidSignature_Returns401()
    {
        var account = await CreateActiveUserAndAccountAsync();
        var token = GenerateToken(account, TimeSpan.FromMinutes(15));
        
        // Tamper token
        token += "tampered";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/test/ProtectedTest");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ExpiredToken_Returns401()
    {
        var account = await CreateActiveUserAndAccountAsync();
        
        var keyProvider = _scope.ServiceProvider.GetRequiredService<IJwtSigningKeyProvider>();
        var activeKey = keyProvider.GetActiveSigningKey();
        var utcNow = DateTime.UtcNow.AddMinutes(-30);
        var expiresAt = utcNow.AddMinutes(15);

        var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, account.UserId.ToString()),
            new System.Security.Claims.Claim("security_stamp", account.SecurityStamp.ToString())
        };

        using var rsa = System.Security.Cryptography.RSA.Create();
        rsa.ImportRSAPrivateKey(activeKey.PrivateKeyBytes, out _);
        var rsaKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa)
        {
            KeyId = activeKey.Kid,
            CryptoProviderFactory = new Microsoft.IdentityModel.Tokens.CryptoProviderFactory { CacheSignatureProviders = false }
        };
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(rsaKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(claims),
            Expires = expiresAt,
            IssuedAt = utcNow,
            NotBefore = utcNow,
            SigningCredentials = credentials,
            Issuer = "PTKD-ERP",
            Audience = "PTKD-ERP-API"
        };

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        var tokenString = handler.WriteToken(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);
        var response = await _client.GetAsync("/api/v2/test/ProtectedTest");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_SecurityStampMismatch_Returns401()
    {
        var account = await CreateActiveUserAndAccountAsync();
        var token = GenerateToken(account, TimeSpan.FromMinutes(15));

        account.InvalidateSessions(Guid.NewGuid(), DateTime.UtcNow);
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/test/ProtectedTest");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        // Assert no internal reason revealed
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("stamp", content);
    }

    [Fact]
    public async Task ProtectedEndpoint_AccountDisabled_Returns401()
    {
        var account = await CreateActiveUserAndAccountAsync();
        var token = GenerateToken(account, TimeSpan.FromMinutes(15));

        account.Disable(DateTime.UtcNow, 1);
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/test/ProtectedTest");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("disabled", content);
    }

    [Fact]
    public async Task ProtectedEndpoint_EmploymentTerminated_Returns401()
    {
        var account = await CreateActiveUserAndAccountAsync();
        var token = GenerateToken(account, TimeSpan.FromMinutes(15));

        var user = await _dbContext.Users.FindAsync(account.UserId);
        typeof(User).GetProperty(nameof(User.EmploymentStatus))?.SetValue(user, "TERMINATED");
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/test/ProtectedTest");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("employment", content);
    }

    [Fact]
    public async Task ProtectedEndpoint_SessionCutoff_Returns401()
    {
        var account = await CreateActiveUserAndAccountAsync();
        
        // Force the stamp to be the same but the cutoff is updated
        var originalStamp = account.SecurityStamp;
        var token = GenerateToken(account, TimeSpan.FromMinutes(15));

        var prop = typeof(UserAuthAccount).GetProperty(nameof(UserAuthAccount.SessionsInvalidatedAt));
        prop?.SetValue(account, DateTime.UtcNow.AddMinutes(1)); // Cutoff in future of token issue
        
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v2/test/ProtectedTest");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
