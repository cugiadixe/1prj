using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class Grave
{
    public const string StatusEmpty = "EMPTY";        // còn trống
    public const string StatusReserved = "RESERVED";  // đã đặt / bán
    public const string StatusOccupied = "OCCUPIED";  // đã an táng
    public const string StatusRelocated = "RELOCATED"; // đã cải táng / bốc

    public const string TypeSingle = "SINGLE";  // mộ đơn — 1 cốt
    public const string TypeDouble = "DOUBLE";  // mộ đôi — 2 cốt
    public const string TypeFamily = "FAMILY";  // mộ gia tộc — ≥3 cốt

    /// <summary>Loại mộ được XÁC ĐỊNH theo số cốt: 1 = đơn, 2 = đôi, ≥3 = gia tộc.</summary>
    public static string TypeForCotCount(int cotCount)
        => cotCount <= 1 ? TypeSingle : cotCount == 2 ? TypeDouble : TypeFamily;

    public long Id { get; private set; }
    public string GraveCode { get; private set; } = null!;
    public string Zone { get; private set; } = null!;
    public string PlotNumber { get; private set; } = null!;
    public string? RowLabel { get; private set; }
    public string? ColLabel { get; private set; }
    public string GraveType { get; private set; } = null!;
    public decimal? AreaM2 { get; private set; }
    public int CotCount { get; private set; }
    public string Status { get; private set; } = null!;
    public long? OwnerCustomerId { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public string? EmergencyContactRelationship { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    public Customer? Owner { get; private set; }
    public ICollection<GraveOccupant> Occupants { get; private set; } = new List<GraveOccupant>();

    private Grave() { }

    public Grave(
        string graveCode, string zone, string plotNumber, string graveType, string status,
        string? rowLabel, string? colLabel, decimal? areaM2, int cotCount, long? ownerCustomerId,
        string? emergencyContactName, string? emergencyContactPhone, string? emergencyContactRelationship,
        string? notes)
    {
        GraveCode = graveCode ?? throw new ArgumentNullException(nameof(graveCode));
        Zone = zone ?? throw new ArgumentNullException(nameof(zone));
        PlotNumber = plotNumber ?? throw new ArgumentNullException(nameof(plotNumber));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        RowLabel = rowLabel;
        ColLabel = colLabel;
        AreaM2 = areaM2;
        CotCount = cotCount <= 0 ? 1 : cotCount;
        // Loại mộ luôn suy từ số cốt — bỏ qua giá trị graveType truyền vào để tránh lệch.
        GraveType = TypeForCotCount(CotCount);
        OwnerCustomerId = ownerCustomerId;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        EmergencyContactRelationship = emergencyContactRelationship;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string zone, string plotNumber, string graveType, string status,
        string? rowLabel, string? colLabel, decimal? areaM2, int cotCount, long? ownerCustomerId,
        string? emergencyContactName, string? emergencyContactPhone, string? emergencyContactRelationship,
        string? notes, long? updatedByUserId)
    {
        Zone = zone ?? throw new ArgumentNullException(nameof(zone));
        PlotNumber = plotNumber ?? throw new ArgumentNullException(nameof(plotNumber));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        RowLabel = rowLabel;
        ColLabel = colLabel;
        AreaM2 = areaM2;
        CotCount = cotCount <= 0 ? 1 : cotCount;
        // Loại mộ luôn suy từ số cốt — bỏ qua giá trị graveType truyền vào để tránh lệch.
        GraveType = TypeForCotCount(CotCount);
        OwnerCustomerId = ownerCustomerId;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        EmergencyContactRelationship = emergencyContactRelationship;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    public void SetCreatedBy(long userId)
    {
        CreatedByUserId = userId;
    }

    public void MarkUpdated(long userId)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = userId;
    }
}
