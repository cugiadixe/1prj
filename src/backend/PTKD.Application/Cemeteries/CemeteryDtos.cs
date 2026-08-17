namespace PTKD.Application.Cemeteries;

public class CemeteryDto
{
    public long Id { get; set; }
    public string CemeteryCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public string? CardWatermarkCode { get; set; }
}

public class SetWatermarkRequest
{
    public string? WatermarkCode { get; set; }
}
