using System;

namespace PTKD.Domain.Entities;

public class Profile
{
    public long Id { get; private set; }
    public string FullName { get; private set; } = null!;
    public string? Cccd { get; private set; }
    public DateTime? Dob { get; private set; }
    public string? DobPartial { get; private set; }
    public string? DobPrecision { get; private set; }
    public string? Gender { get; private set; }
    public string? PermanentAddress { get; private set; }
    public DateTime? CccdIssueDate { get; private set; }
    public string? CccdIssuePlace { get; private set; }
    public string? TaxCode { get; private set; }
    public string? Phone { get; private set; }
    public string? ContactAddress { get; private set; }
    public DateTime? DeathDateSolar { get; private set; }
    public string? DeathDateLunar { get; private set; }
    public string? DeathPlace { get; private set; }
    public string? Hometown { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private Profile() { }

    public Profile(
        string fullName, string? cccd, DateTime? dob, string? dobPartial, string? dobPrecision,
        string? gender, string? permanentAddress, DateTime? cccdIssueDate, string? cccdIssuePlace,
        string? taxCode, string? phone, string? contactAddress,
        DateTime? deathDateSolar, string? deathDateLunar, string? deathPlace, string? hometown)
    {
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Cccd = cccd;
        Dob = dob;
        DobPartial = dobPartial;
        DobPrecision = dobPrecision;
        Gender = gender;
        PermanentAddress = permanentAddress;
        CccdIssueDate = cccdIssueDate;
        CccdIssuePlace = cccdIssuePlace;
        TaxCode = taxCode;
        Phone = phone;
        ContactAddress = contactAddress;
        DeathDateSolar = deathDateSolar;
        DeathDateLunar = deathDateLunar;
        DeathPlace = deathPlace;
        Hometown = hometown;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string fullName, string? cccd, DateTime? dob, string? dobPartial, string? dobPrecision,
        string? gender, string? permanentAddress, DateTime? cccdIssueDate, string? cccdIssuePlace,
        string? taxCode, string? phone, string? contactAddress,
        DateTime? deathDateSolar, string? deathDateLunar, string? deathPlace, string? hometown,
        long? updatedByUserId)
    {
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Cccd = cccd;
        Dob = dob;
        DobPartial = dobPartial;
        DobPrecision = dobPrecision;
        Gender = gender;
        PermanentAddress = permanentAddress;
        CccdIssueDate = cccdIssueDate;
        CccdIssuePlace = cccdIssuePlace;
        TaxCode = taxCode;
        Phone = phone;
        ContactAddress = contactAddress;
        DeathDateSolar = deathDateSolar;
        DeathDateLunar = deathDateLunar;
        DeathPlace = deathPlace;
        Hometown = hometown;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    public void SetCreatedBy(long userId)
    {
        CreatedByUserId = userId;
    }

    public void Deactivate(long? updatedByUserId)
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    /// <summary>Ghi nhận ngày mất (dùng khi đánh dấu khách hàng qua đời).</summary>
    public void MarkDeceased(DateTime? deathDateSolar, long? updatedByUserId)
    {
        if (deathDateSolar.HasValue)
            DeathDateSolar = deathDateSolar;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
