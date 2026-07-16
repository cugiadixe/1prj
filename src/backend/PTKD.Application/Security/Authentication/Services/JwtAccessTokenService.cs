using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Application.Security.Authentication.Services;

public sealed class JwtAccessTokenService : IJwtAccessTokenService
{
    private readonly IJwtSigningKeyProvider _keyProvider;
    private readonly TimeProvider _timeProvider;

    public JwtAccessTokenService(IJwtSigningKeyProvider keyProvider, TimeProvider timeProvider)
    {
        _keyProvider = keyProvider;
        _timeProvider = timeProvider;
    }

    public JwtIssuanceResult IssueAccessToken(AccessTokenRequest request)
    {
        var activeKey = _keyProvider.GetActiveSigningKey();
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = utcNow.AddMinutes(15);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new Claim("auth_account_id", request.AccountId.ToString()),
            new Claim("sid", request.SessionId.ToString()),
            new Claim("fid", request.FamilyId.ToString()),
            new Claim("security_stamp", request.SecurityStamp.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            claims.Add(new Claim("login_name", request.Username));
        }

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(activeKey.PrivateKeyBytes, out _);
        var rsaKey = new RsaSecurityKey(rsa)
        {
            KeyId = activeKey.Kid,
            // Disable provider caching: the RSA object is owned by the using block
            // and will be disposed at end of method. A cached AsymmetricSignatureProvider
            // would hold a stale reference and throw ObjectDisposedException on next call.
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
        var credentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            IssuedAt = utcNow,
            NotBefore = utcNow,
            Issuer = "PTKD-ERP",
            Audience = "PTKD-ERP-API",
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        var jwtString = handler.WriteToken(token);

        return new JwtIssuanceResult(jwtString, expiresAt);
    }

    public JwtValidationResult? ValidateAccessToken(string jwt)
    {
        var validationKeys = _keyProvider.GetValidationKeys();
        var securityKeys = new List<SecurityKey>();
        var rsaInstances = new List<RSA>();

        try
        {
            foreach (var key in validationKeys)
            {
                var rsa = RSA.Create();
                rsaInstances.Add(rsa);
                rsa.ImportRSAPublicKey(key.PublicKeyBytes, out _);
                securityKeys.Add(new RsaSecurityKey(rsa)
                {
                    KeyId = key.Kid,
                    CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
                });
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = securityKeys,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "login_name",
                LifetimeValidator = (notBefore, expires, token, param) =>
                {
                    if (expires != null)
                    {
                        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
                        return expires.Value.Add(param.ClockSkew) >= utcNow;
                    }
                    return false;
                }
            };

            var handler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };
            var principal = handler.ValidateToken(jwt, validationParameters, out _);

            var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var accountIdString = principal.FindFirst("auth_account_id")?.Value;
            var sidString = principal.FindFirst("sid")?.Value;
            var fidString = principal.FindFirst("fid")?.Value;
            var stampString = principal.FindFirst("security_stamp")?.Value;
            var username = principal.FindFirst("login_name")?.Value ?? string.Empty;
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;

            if (long.TryParse(userIdString, out var userId) &&
                long.TryParse(accountIdString, out var accountId) &&
                Guid.TryParse(sidString, out var sid) &&
                Guid.TryParse(fidString, out var fid) &&
                Guid.TryParse(stampString, out var stamp))
            {
                return new JwtValidationResult(userId, accountId, sid, fid, stamp, username, jti);
            }

            throw new Exception($"Validation failed. userId: {userIdString}, accountId: {accountIdString}, sid: {sidString}, fid: {fidString}, stamp: {stampString}, username: {username}, jti: {jti}");
        }
        // no catch block
        finally
        {
            foreach (var rsa in rsaInstances)
            {
                rsa.Dispose();
            }
        }
    }
}
