using System;

namespace PTKD.Application.Cards.DTOs;

/// <summary>Tạo/cấp thẻ mộ mới từ một phần mộ. Công ty của thẻ suy từ nghĩa trang của mộ.</summary>
public class CreateCardRequest
{
    public long GraveId { get; set; }
    public long? ServiceId { get; set; }
}

public class CardDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string? GraveId { get; set; }
    public string? CardNumber { get; set; }
    public long? ServiceId { get; set; }
    public int PrintCount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
