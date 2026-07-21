using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Application.Security.Authentication.Services;
using PTKD.Infrastructure.Security.Cryptography;
using Xunit;

namespace PTKD.UnitTests.Security.Authentication;

public class JwtAccessTokenServiceTests
{
    private readonly JwtSigningKeyProvider _keyProvider;
    private readonly FakeTimeProvider _timeProvider;
    private readonly JwtAccessTokenService _service;

    public JwtAccessTokenServiceTests()
    {
        _keyProvider = new JwtSigningKeyProvider();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 7, 16, 0, 0, 0, TimeSpan.Zero));
        _service = new JwtAccessTokenService(_keyProvider, _timeProvider);
    }

    [Fact]
    public void IssueAccessToken_ContainsRequiredClaimsOnly_AndNoPermissions()
    {
        var request = new AccessTokenRequest(
            123,
            456,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "testuser",
            false);

        var result = _service.IssueAccessToken(request);

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAtUtc.Should().Be(_timeProvider.GetUtcNow().UtcDateTime.AddMinutes(15));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);

        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "123");
        token.Claims.Should().Contain(c => c.Type == "auth_account_id" && c.Value == "456");
        token.Claims.Should().Contain(c => c.Type == "sid" && c.Value == request.SessionId.ToString());
        token.Claims.Should().Contain(c => c.Type == "fid" && c.Value == request.FamilyId.ToString());
        token.Claims.Should().Contain(c => c.Type == "security_stamp" && c.Value == request.SecurityStamp.ToString());
        token.Claims.Should().Contain(c => c.Type == "login_name" && c.Value == "testuser");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        
        // Ensure no permissions or scopes
        token.Claims.Should().NotContain(c => c.Type == "permissions" || c.Type == "role");
    }

    [Fact]
    public void ValidateAccessToken_ReturnsValidResult_ForValidToken()
    {
        var request = new AccessTokenRequest(
            123,
            456,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "testuser",
            false);

        var issueResult = _service.IssueAccessToken(request);
        
        var validationResult = _service.ValidateAccessToken(issueResult.Token);

        validationResult.Should().NotBeNull();
        validationResult!.UserId.Should().Be(123);
        validationResult.AccountId.Should().Be(456);
        validationResult.SessionId.Should().Be(request.SessionId);
        validationResult.FamilyId.Should().Be(request.FamilyId);
        validationResult.SecurityStamp.Should().Be(request.SecurityStamp);
        validationResult.Username.Should().Be("testuser");
        validationResult.TokenId.Should().NotBeNullOrWhiteSpace();
    }
}
