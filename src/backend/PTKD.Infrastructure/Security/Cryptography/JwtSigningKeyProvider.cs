using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Infrastructure.Security.Cryptography;

public sealed class JwtSigningKeyProvider : IJwtSigningKeyProvider, IDisposable
{
    private readonly RSA _rsa;
    private readonly string _kid;
    private readonly byte[] _privateKey;
    private readonly byte[] _publicKey;

    public JwtSigningKeyProvider()
    {
        // Simple in-memory key for Dev/Testing.
        _rsa = RSA.Create(2048);
        _kid = Guid.NewGuid().ToString("N");
        _privateKey = _rsa.ExportRSAPrivateKey();
        _publicKey = _rsa.ExportRSAPublicKey();
    }

    public SigningKeyDescriptor GetActiveSigningKey()
    {
        return new SigningKeyDescriptor(_kid, _privateKey);
    }

    public IReadOnlyList<ValidationKeyDescriptor> GetValidationKeys()
    {
        return new List<ValidationKeyDescriptor>
        {
            new ValidationKeyDescriptor(_kid, _publicKey)
        };
    }

    public void Dispose()
    {
        _rsa?.Dispose();
    }
}
