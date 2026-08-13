using System;

namespace PTKD.Domain.Entities;

public class GraveOccupant
{
    public long Id { get; private set; }
    public long GraveId { get; private set; }
    public long? DeceasedCustomerId { get; private set; }   // cốt LÀ khách hàng (status DECEASED)
    public string FullName { get; private set; } = null!;
    public string? Gender { get; private set; }
    public DateTime? Dob { get; private set; }
    public DateTime? DeathDateSolar { get; private set; }
    public string? DeathDateLunar { get; private set; }
    public DateTime? BurialDate { get; private set; }
    public string? Hometown { get; private set; }
    public string? OwnerRelationship { get; private set; }     // chủ mộ → người mất
    public string? DeceasedRelationship { get; private set; }  // người mất → chủ mộ
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private GraveOccupant() { }

    public GraveOccupant(
        long graveId, string fullName, string? gender, DateTime? dob,
        DateTime? deathDateSolar, string? deathDateLunar, DateTime? burialDate,
        string? hometown, string? ownerRelationship, string? deceasedRelationship, string? notes)
    {
        GraveId = graveId;
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Gender = gender;
        Dob = dob;
        DeathDateSolar = deathDateSolar;
        DeathDateLunar = deathDateLunar;
        BurialDate = burialDate;
        Hometown = hometown;
        OwnerRelationship = ownerRelationship;
        DeceasedRelationship = deceasedRelationship;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string fullName, string? gender, DateTime? dob,
        DateTime? deathDateSolar, string? deathDateLunar, DateTime? burialDate,
        string? hometown, string? ownerRelationship, string? deceasedRelationship, string? notes, long? updatedByUserId)
    {
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Gender = gender;
        Dob = dob;
        DeathDateSolar = deathDateSolar;
        DeathDateLunar = deathDateLunar;
        BurialDate = burialDate;
        Hometown = hometown;
        OwnerRelationship = ownerRelationship;
        DeceasedRelationship = deceasedRelationship;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    public void SetCreatedBy(long userId)
    {
        CreatedByUserId = userId;
    }

    /// <summary>Cập nhật nhãn quan hệ 2 chiều (dùng khi tái suy diễn lúc đổi chủ mộ).</summary>
    public void SetDerivedRelationship(string? ownerRelationship, string? deceasedRelationship, long? updatedByUserId)
    {
        OwnerRelationship = ownerRelationship;
        DeceasedRelationship = deceasedRelationship;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
