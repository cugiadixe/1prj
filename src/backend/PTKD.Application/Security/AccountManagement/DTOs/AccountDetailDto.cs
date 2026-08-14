namespace PTKD.Application.Security.AccountManagement.DTOs;

// Safe projection of UserAuthAccount. Excludes: PasswordHash, SecurityStamp,
// SessionsInvalidatedAt, RowVersion, PasswordHistories.
public sealed record AccountDetailDto
{
    public required long Id { get; init; }
    public required long UserId { get; init; }
    public required string ProviderType { get; init; }
    public required string Username { get; init; }
    public required string Status { get; init; }
    public required bool IsInternalProvider { get; init; }
    public required int FailedAttemptCount { get; init; }
    public required bool IsManualLock { get; init; }
    public DateTime? LockoutEnd { get; init; }
    public required bool MustChangePassword { get; init; }
    public DateTime? TemporaryPasswordExpiresAt { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    // Công ty/phòng ban chính của nhân viên (đồng bộ với danh sách). NULL nếu chưa phân công.
    public string? CompanyName { get; init; }
    public string? DepartmentName { get; init; }
}
