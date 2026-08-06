using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Application.Security.Authentication.Services;

public sealed class RefreshTokenMaterialService : IRefreshTokenMaterialService
{
    public (string RawMaterial, string Hash) Generate()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        var rawMaterial = Base64UrlEncoder.Encode(randomBytes);
        var hash = ComputeHash(rawMaterial);
        
        return (rawMaterial, hash);
    }

    public string ComputeHash(string rawMaterial)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawMaterial);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToUpperInvariant();
    }
}
