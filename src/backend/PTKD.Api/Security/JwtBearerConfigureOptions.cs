using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Api.Security;

public sealed class JwtBearerConfigureOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IJwtSigningKeyProvider _keyProvider;

    public JwtBearerConfigureOptions(IJwtSigningKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        options.RequireHttpsMetadata = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "PTKD-ERP",

            ValidateAudience = true,
            ValidAudience = "PTKD-ERP-API",

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                var keyDesc = _keyProvider.GetValidationKeys().FirstOrDefault(k => k.Kid == kid);
                if (keyDesc != null)
                {
                    var rsa = RSA.Create();
                    rsa.ImportRSAPublicKey(keyDesc.PublicKeyBytes, out _);
                    return new[] { new RsaSecurityKey(rsa) { KeyId = kid } };
                }
                return Enumerable.Empty<SecurityKey>();
            }
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (principal == null)
                {
                    context.Fail("Unauthorized");
                    return;
                }

                var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                var stampClaim = principal.FindFirst("security_stamp")?.Value;
                
                // Get 'iat' claim natively parsed by JwtSecurityTokenHandler or extract from ValidFrom
                // In System.IdentityModel.Tokens.Jwt, iat maps to 'iat' or ValidFrom depending on configuration.
                var issuedAtUtc = context.SecurityToken.ValidFrom;

                if (string.IsNullOrEmpty(subClaim) || !long.TryParse(subClaim, out var userId))
                {
                    context.Fail("Unauthorized");
                    return;
                }

                if (string.IsNullOrEmpty(stampClaim) || !Guid.TryParse(stampClaim, out var securityStamp))
                {
                    context.Fail("Unauthorized");
                    return;
                }

                var validator = context.HttpContext.RequestServices.GetRequiredService<IProtectedRequestValidator>();
                
                bool isValid = await validator.ValidateAsync(userId, securityStamp, issuedAtUtc, context.HttpContext.RequestAborted);
                
                if (!isValid)
                {
                    context.Fail("Unauthorized");
                    return;
                }
            }
        };
    }
}
