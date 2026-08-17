using System;
using System.Collections.Generic;

namespace PTKD.Application.Organizations.Users.DTOs;

public sealed record OrgBriefDto(long Id, string Name);

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

    /// <summary>Công ty người dùng đang được phân công (còn hiệu lực) — để hiển thị/lọc.</summary>
    public List<OrgBriefDto> Companies { get; set; } = new();
    /// <summary>Phòng ban người dùng đang được phân công (còn hiệu lực).</summary>
    public List<OrgBriefDto> Departments { get; set; } = new();
}
