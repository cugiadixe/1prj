namespace PTKD.Application.Security.Authentication.Interfaces;

/// <summary>
/// Generates opaque refresh token raw material and computes its SHA-256 hash.
/// Raw material must never be persisted or logged; only the hash is stored.
/// </summary>
public interface IRefreshTokenMaterialService
{
    /// <summary>
    /// Generates cryptographically secure random material (≥ 256 bits, Base64Url encoded)
    /// and returns both the raw material (for transport via cookie) and its
    /// uppercase hex SHA-256 hash (for storage in dbo.Refresh_Tokens.token_hash).
    /// </summary>
    (string RawMaterial, string Hash) Generate();

    /// <summary>
    /// Computes the SHA-256 hash of the supplied raw material.
    /// Returns the uppercase hex string (64 chars) for lookup in dbo.Refresh_Tokens.
    /// </summary>
    string ComputeHash(string rawMaterial);
}
