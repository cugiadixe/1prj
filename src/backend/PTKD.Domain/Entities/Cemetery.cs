using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

/// <summary>
/// Nghĩa trang — thuộc đúng MỘT công ty. Mộ thuộc công ty QUA nghĩa trang (không gắn công ty
/// thẳng vào mộ), nên đây là điểm neo để lọc mộ theo công ty.
/// </summary>
public class Cemetery
{
    public long Id { get; private set; }
    public string CemeteryCode { get; private set; } = null!;
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    public Company? Company { get; private set; }
    public ICollection<Grave> Graves { get; private set; } = new List<Grave>();

    private Cemetery() { } // EF Core

    public Cemetery(string cemeteryCode, long companyId, string name, string? address)
    {
        CemeteryCode = cemeteryCode ?? throw new ArgumentNullException(nameof(cemeteryCode));
        CompanyId = companyId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? address, long? updatedByUserId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    public void SetStatus(bool isActive, long? updatedByUserId)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    public void SetCreatedBy(long userId) => CreatedByUserId = userId;
}
