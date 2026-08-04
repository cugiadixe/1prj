using System;

namespace PTKD.Domain.Entities;

public class CarePackageRequestItem
{
    public long Id { get; private set; }
    public long CarePackageRequestId { get; private set; }
    public CarePackageRequest CarePackageRequest { get; private set; } = null!;
    
    public string? GraveId { get; private set; }
    public int CotCountSnapshot { get; private set; }
    public DateTime ServicePeriodStartDate { get; private set; }
    public DateTime ServicePeriodEndDate { get; private set; }
    public decimal UnitPriceSnapshot { get; private set; }
    public decimal LineSubtotal { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private CarePackageRequestItem() { }

    public static CarePackageRequestItem Create(
        string? graveId,
        int cotCountSnapshot,
        DateTime servicePeriodStartDate,
        DateTime servicePeriodEndDate,
        decimal unitPriceSnapshot,
        string? notes = null)
    {
        if (cotCountSnapshot <= 0)
            throw new ArgumentException("Cot count must be positive.", nameof(cotCountSnapshot));

        if (unitPriceSnapshot < 0)
            throw new ArgumentException("Unit price snapshot cannot be negative.", nameof(unitPriceSnapshot));

        // Validate one year period
        // End date should be start date + 1 year, often minus 1 day depending on business rule,
        // but exact 1 year is the minimal validation for Phase 1B.9 supports one-year packages only.
        var expectedEndDate = servicePeriodStartDate.AddYears(1);
        if (servicePeriodEndDate.Date != expectedEndDate.Date && servicePeriodEndDate.Date != expectedEndDate.AddDays(-1).Date)
            throw new ArgumentException("Care package service period must be exactly one year for Phase 1B.9.");

        return new CarePackageRequestItem
        {
            GraveId = graveId,
            CotCountSnapshot = cotCountSnapshot,
            ServicePeriodStartDate = servicePeriodStartDate,
            ServicePeriodEndDate = servicePeriodEndDate,
            UnitPriceSnapshot = unitPriceSnapshot,
            LineSubtotal = unitPriceSnapshot * cotCountSnapshot, // 1 year multiplier is implicit here based on rule formula
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
