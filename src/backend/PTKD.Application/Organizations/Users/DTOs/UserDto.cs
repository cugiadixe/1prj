using System;

namespace PTKD.Application.Organizations.Users.DTOs;

public class UserDto
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string EmploymentStatus { get; set; } = null!;
    public string AccountStatus { get; set; } = null!;
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
