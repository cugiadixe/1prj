using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace PTKD.Api.Security;

/// <summary>
/// Implements CSRF protection using the double-submit cookie pattern.
/// A random opaque token is generated, set as a non-HttpOnly cookie, and also
/// returned in an X-CSRF-Token response header. On subsequent cookie-reliant
/// requests the client must send matching cookie and header values.
/// </summary>
public sealed class CsrfTokenService
{
    private const string CookieName = "X-CSRF-TOKEN";
    private const string HeaderName = "X-CSRF-Token";
    private const string CookiePath = "/";

    /// <summary>Generates a new CSRF token, writes cookie and response header.</summary>
    public string Issue(HttpResponse response)
    {
        var token = GenerateToken();
        SetCookie(response, token, DateTimeOffset.UtcNow.AddDays(7));
        response.Headers.Append(HeaderName, token);
        return token;
    }

    /// <summary>Validates the CSRF token from header vs cookie using constant-time comparison.</summary>
    public bool Validate(HttpRequest request)
    {
        var headerToken = request.Headers[HeaderName].ToString();
        var cookieToken = request.Cookies[CookieName] ?? string.Empty;

        if (string.IsNullOrEmpty(headerToken) || string.IsNullOrEmpty(cookieToken))
            return false;

        // Constant-time comparison to avoid timing attacks
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(headerToken),
            System.Text.Encoding.UTF8.GetBytes(cookieToken));
    }

    /// <summary>Deletes the CSRF cookie on logout.</summary>
    public void Delete(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            Path = CookiePath,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static void SetCookie(HttpResponse response, string token, DateTimeOffset expires)
    {
        response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = false,   // Must be readable by JavaScript (for SPA to read and set header)
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Expires = expires
        });
    }
}
