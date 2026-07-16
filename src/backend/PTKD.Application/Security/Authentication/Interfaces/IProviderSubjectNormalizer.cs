namespace PTKD.Application.Security.Authentication.Interfaces;

public interface IProviderSubjectNormalizer
{
    ProviderIdentity Normalize(string providerType, string providerSubject);
}

public readonly record struct ProviderIdentity(string ProviderType, string ProviderSubject);
