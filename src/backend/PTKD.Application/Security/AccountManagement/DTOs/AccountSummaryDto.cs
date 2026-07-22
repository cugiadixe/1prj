namespace PTKD.Application.Security.AccountManagement.DTOs;

// Safe projection for account discovery. Excludes: PasswordHash, SecurityStamp,
// SessionsInvalidatedAt, RowVersion, PasswordHistories, User.Email.
public sealed record AccountSummaryDto
{
    public required long AccountId { get; init; }
    public required long UserId { get; init; }
    public required string Username { get; init; }
    public required string ProviderType { get; init; }
    public required string Status { get; init; }
    public required bool MustChangePassword { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FullName { get; init; }
    public required string EmploymentStatus { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
