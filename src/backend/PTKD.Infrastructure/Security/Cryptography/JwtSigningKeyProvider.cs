using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Infrastructure.Security.Cryptography;

/// <summary>
/// Nhà cung cấp khoá ký RS256.
///
/// Production PHẢI nạp một khoá RSA BỀN qua cấu hình để token còn hiệu lực sau khi khởi động lại
/// và để nhiều tiến trình/bản sao cùng xác thực được token của nhau. Cấu hình (ưu tiên trên xuống):
///   • <c>Jwt:SigningKeyPath</c> — đường dẫn tới file PEM chứa RSA private key (PKCS#8 hoặc PKCS#1).
///   • <c>Jwt:SigningKeyPem</c>  — nội dung PEM đặt thẳng trong cấu hình/biến môi trường/secret.
/// Khi không cấu hình khoá nào (chỉ dành cho Dev/Test) thì sinh khoá tạm trong bộ nhớ và GHI CẢNH
/// BÁO — restart sẽ vô hiệu mọi token đã cấp. Không nhúng private key vào mã nguồn.
///
/// <c>kid</c> của khoá bền được suy TẤT ĐỊNH từ chính khoá công khai, nên cùng một khoá luôn cho
/// cùng một kid qua các lần khởi động — bắt buộc để token cũ còn khớp key khi validate.
/// </summary>
public sealed class JwtSigningKeyProvider : IJwtSigningKeyProvider, IDisposable
{
    private readonly RSA _rsa;
    private readonly string _kid;
    private readonly byte[] _privateKey;
    private readonly byte[] _publicKey;

    public JwtSigningKeyProvider(IConfiguration configuration, ILogger<JwtSigningKeyProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        var pem = ResolveConfiguredPem(configuration);

        if (!string.IsNullOrWhiteSpace(pem))
        {
            _rsa = RSA.Create();
            try
            {
                _rsa.ImportFromPem(pem);
            }
            catch (Exception ex)
            {
                // Fail-closed: cấu hình khoá sai còn hơn âm thầm rơi về khoá tạm ở production.
                throw new InvalidOperationException(
                    "Không nạp được khoá ký JWT từ cấu hình (Jwt:SigningKeyPath / Jwt:SigningKeyPem). " +
                    "Kiểm tra PEM có phải RSA private key hợp lệ (PKCS#8 hoặc PKCS#1).", ex);
            }

            if (_rsa.KeySize < 2048)
            {
                throw new InvalidOperationException(
                    $"Khoá ký JWT chỉ {_rsa.KeySize}-bit; yêu cầu tối thiểu 2048-bit.");
            }

            _publicKey = _rsa.ExportRSAPublicKey();
            _privateKey = _rsa.ExportRSAPrivateKey();
            _kid = DeriveKid(_publicKey);

            logger.LogInformation("Đã nạp khoá ký JWT bền từ cấu hình (kid={Kid}).", _kid);
        }
        else
        {
            // Chỉ dùng cho Dev/Test — KHÔNG bền qua restart, không dùng cho production.
            _rsa = RSA.Create(2048);
            _publicKey = _rsa.ExportRSAPublicKey();
            _privateKey = _rsa.ExportRSAPrivateKey();
            _kid = DeriveKid(_publicKey);

            logger.LogWarning(
                "Chưa cấu hình khoá ký JWT bền (Jwt:SigningKeyPath / Jwt:SigningKeyPem) — đang dùng " +
                "khoá TẠM sinh trong bộ nhớ. Mọi token sẽ bị vô hiệu khi khởi động lại và nhiều bản " +
                "sao sẽ không xác thực token của nhau. KHÔNG dùng cấu hình này cho production.");
        }
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

    private static string? ResolveConfiguredPem(IConfiguration configuration)
    {
        var path = configuration["Jwt:SigningKeyPath"];
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    $"Jwt:SigningKeyPath trỏ tới file không tồn tại: {path}");
            }
            return File.ReadAllText(path);
        }

        var pem = configuration["Jwt:SigningKeyPem"];
        return string.IsNullOrWhiteSpace(pem) ? null : pem;
    }

    // kid tất định = 16 ký tự hex đầu của SHA-256(public key). Cùng khoá → cùng kid mọi lần chạy.
    private static string DeriveKid(byte[] publicKey)
    {
        var hash = SHA256.HashData(publicKey);
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    public void Dispose()
    {
        _rsa?.Dispose();
    }
}
