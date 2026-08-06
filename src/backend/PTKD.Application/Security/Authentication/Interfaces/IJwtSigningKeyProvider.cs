namespace PTKD.Application.Security.Authentication.Interfaces;

/// <summary>
/// Represents the resolved RS256 signing key and its Key ID (kid).
/// </summary>
public sealed record SigningKeyDescriptor(string Kid, byte[] PrivateKeyBytes);

/// <summary>
/// Represents an RS256 validation key entry that may be used to verify signatures.
/// </summary>
public sealed record ValidationKeyDescriptor(string Kid, byte[] PublicKeyBytes);

/// <summary>
/// Abstraction for resolving JWT signing and validation keys.
/// Implementations must:
///  - Return the active signing key for issuance.
///  - Return all current validation keys (active + any keys within the 20-minute overlap window).
///  - Never embed production private key material in source code.
///  - Fail closed (throw) if no signing key is available at startup.
/// </summary>
public interface IJwtSigningKeyProvider
{
    /// <summary>Gets the active signing key used for new token issuance.</summary>
    SigningKeyDescriptor GetActiveSigningKey();

    /// <summary>Gets all validation keys, including those in the overlap/grace window.</summary>
    IReadOnlyList<ValidationKeyDescriptor> GetValidationKeys();
}
