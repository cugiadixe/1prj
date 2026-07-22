namespace PTKD.Application.Security.AccountManagement;

public sealed class AccountSearchParameters
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? ProviderType { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
