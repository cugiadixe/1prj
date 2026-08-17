using System;

namespace PTKD.Domain.Entities;

/// <summary>
/// Mẫu hoa văn (watermark) tải lên — thư viện dùng chung TRONG một công ty. Nghĩa trang chọn mẫu
/// qua mã "UPLOAD:{id}". Ảnh lưu thẳng trong DB (số lượng ít, nhỏ) để khỏi phụ thuộc đường dẫn file.
/// </summary>
public class CardWatermark
{
    public long Id { get; private set; }
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public byte[] ImageBytes { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }

    private CardWatermark() { } // EF Core

    public static CardWatermark Create(long companyId, string name, string contentType, byte[] imageBytes, long actorUserId)
    {
        return new CardWatermark
        {
            CompanyId = companyId,
            Name = name,
            ContentType = contentType,
            ImageBytes = imageBytes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = actorUserId,
        };
    }
}
