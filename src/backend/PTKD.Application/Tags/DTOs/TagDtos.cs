using System;

namespace PTKD.Application.Tags.DTOs;

public class TagDto
{
    public long Id { get; set; }
    public string TagType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class CreateTagRequest
{
    public string TagType { get; set; } = null!;   // CUSTOMER | GRAVE
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
}

public class UpdateTagRequest
{
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public string TargetVersion { get; set; } = null!;
}

/// <summary>Đặt lại toàn bộ tập thẻ cho một đối tượng (khách hàng / mộ). Cho phép tạo thẻ mới theo tên.</summary>
public class SetEntityTagsRequest
{
    public long[] TagIds { get; set; } = Array.Empty<long>();
    public string[] NewTagNames { get; set; } = Array.Empty<string>();
}
