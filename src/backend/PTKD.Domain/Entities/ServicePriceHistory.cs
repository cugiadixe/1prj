using System;

namespace PTKD.Domain.Entities;

public class ServicePriceHistory
{
    public long Id { get; private set; }
    public long ServiceTypeId { get; private set; }
    public decimal Price { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public long ChangedByUserId { get; private set; }
    public string ChangeReason { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private ServicePriceHistory() { }

    public ServicePriceHistory(
        long serviceTypeId,
        decimal price,
        DateTime effectiveFrom,
        long changedByUserId,
        string changeReason)
    {
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));
        if (string.IsNullOrWhiteSpace(changeReason))
            throw new ArgumentException("Change reason is required.", nameof(changeReason));

        ServiceTypeId = serviceTypeId;
        Price = price;
        EffectiveFrom = effectiveFrom;
        ChangedByUserId = changedByUserId;
        ChangeReason = changeReason;
        CreatedAt = DateTime.UtcNow;
    }

    public void CloseEffectivePeriod(DateTime effectiveTo)
    {
        EffectiveTo = effectiveTo;
    }
}
