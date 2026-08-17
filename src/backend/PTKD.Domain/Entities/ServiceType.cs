using System;

namespace PTKD.Domain.Entities;

public class ServiceType
{
    // Cách tính giá của gói: theo cốt (nhân số cốt) hay theo phần mộ (không nhân).
    public const string PricingBasisPerCot = "PER_COT";
    public const string PricingBasisPerGrave = "PER_GRAVE";

    private static string NormalizePricingBasis(string? value)
        => value == PricingBasisPerGrave ? PricingBasisPerGrave : PricingBasisPerCot;

    public long Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal StandardPrice { get; private set; }
    public string StandardPriceCurrency { get; private set; } = null!;
    public int? CycleDurationMonths { get; private set; }
    public bool IsCarePackage { get; private set; }
    /// <summary>PER_COT (mặc định) = giá × số cốt; PER_GRAVE = giá theo phần mộ (× 1).</summary>
    public string PricingBasis { get; private set; } = PricingBasisPerCot;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long CreatedByUserId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private ServiceType() { }

    public ServiceType(
        string code,
        string name,
        string? description,
        decimal standardPrice,
        int? cycleDurationMonths,
        bool isCarePackage,
        long createdByUserId,
        string? pricingBasis = PricingBasisPerCot)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (code.Length > 50)
            throw new ArgumentException("Code must not exceed 50 characters.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Name must not exceed 200 characters.", nameof(name));
        if (standardPrice <= 0)
            throw new ArgumentException("Standard price must be greater than zero.", nameof(standardPrice));
        if (cycleDurationMonths.HasValue && cycleDurationMonths.Value <= 0)
            throw new ArgumentException("Cycle duration months must be greater than zero.", nameof(cycleDurationMonths));

        Code = code;
        Name = name;
        Description = description;
        StandardPrice = standardPrice;
        StandardPriceCurrency = "VND";
        CycleDurationMonths = cycleDurationMonths;
        IsCarePackage = isCarePackage;
        PricingBasis = NormalizePricingBasis(pricingBasis);
        IsActive = true;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? description, int? cycleDurationMonths, bool isCarePackage, string? pricingBasis = PricingBasisPerCot)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Name must not exceed 200 characters.", nameof(name));
        if (cycleDurationMonths.HasValue && cycleDurationMonths.Value <= 0)
            throw new ArgumentException("Cycle duration months must be greater than zero.", nameof(cycleDurationMonths));

        Name = name;
        Description = description;
        CycleDurationMonths = cycleDurationMonths;
        IsCarePackage = isCarePackage;
        PricingBasis = NormalizePricingBasis(pricingBasis);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStandardPrice(decimal price, string reason, long changedByUserId)
    {
        if (price <= 0)
            throw new ArgumentException("Standard price must be greater than zero.", nameof(price));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Change reason is required.", nameof(reason));

        StandardPrice = price;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
