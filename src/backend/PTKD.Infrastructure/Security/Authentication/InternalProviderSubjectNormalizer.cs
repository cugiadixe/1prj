using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Security.Authentication;

namespace PTKD.Infrastructure.Security.Authentication;

public sealed class InternalProviderSubjectNormalizer : IProviderSubjectNormalizer
{
    public ProviderIdentity Normalize(string providerType, string providerSubject)
    {
        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("Provider type is required.", nameof(providerType));

        var normalizedProviderType = providerType.Trim().ToUpperInvariant();
        if (normalizedProviderType.Length > 30)
            throw new ArgumentException("Provider type exceeds the accepted storage length.", nameof(providerType));
        if (string.IsNullOrWhiteSpace(providerSubject))
            throw new ArgumentException("Provider subject is required.", nameof(providerSubject));

        var normalizedProviderSubject = string.Equals(
            normalizedProviderType,
            AuthenticationAccountPolicy.InternalProviderType,
            StringComparison.Ordinal)
            ? providerSubject.Trim().ToUpperInvariant()
            : providerSubject;

        if (normalizedProviderSubject.Length > 200)
            throw new ArgumentException("Provider subject exceeds the accepted storage length.", nameof(providerSubject));

        return new ProviderIdentity(normalizedProviderType, normalizedProviderSubject);
    }
}
