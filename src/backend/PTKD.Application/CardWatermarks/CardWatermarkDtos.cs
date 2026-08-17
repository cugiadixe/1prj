using System;

namespace PTKD.Application.CardWatermarks;

public class CardWatermarkDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    /// <summary>Mã dùng để gán cho nghĩa trang.</summary>
    public string Code => $"UPLOAD:{Id}";
}

public class CardWatermarkContent
{
    public byte[] Bytes { get; set; } = null!;
    public string ContentType { get; set; } = null!;
}
