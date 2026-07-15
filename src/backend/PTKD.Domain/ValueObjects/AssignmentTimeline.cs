using System;

namespace PTKD.Domain.ValueObjects;

public sealed class AssignmentTimeline
{
    public DateTime EffectiveFrom { get; }
    public DateTime? EffectiveTo { get; }

    private AssignmentTimeline(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public static AssignmentTimeline Create(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new ArgumentException("EffectiveTo must be strictly greater than EffectiveFrom.");

        return new AssignmentTimeline(effectiveFrom, effectiveTo);
    }

    public bool Contains(DateTime date)
    {
        if (date < EffectiveFrom) return false;
        if (EffectiveTo.HasValue && date >= EffectiveTo.Value) return false;
        return true;
    }

    public bool Contains(AssignmentTimeline other)
    {
        if (other.EffectiveFrom < EffectiveFrom) return false;
        
        if (EffectiveTo.HasValue)
        {
            if (!other.EffectiveTo.HasValue) return false;
            if (other.EffectiveTo.Value > EffectiveTo.Value) return false;
        }

        return true;
    }

    public bool Overlaps(AssignmentTimeline other)
    {
        var maxFrom = EffectiveFrom > other.EffectiveFrom ? EffectiveFrom : other.EffectiveFrom;

        var minTo = EffectiveTo;
        if (other.EffectiveTo.HasValue)
        {
            if (!minTo.HasValue || other.EffectiveTo.Value < minTo.Value)
            {
                minTo = other.EffectiveTo;
            }
        }

        if (!minTo.HasValue) return true;

        return maxFrom < minTo.Value;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = (AssignmentTimeline)obj;
        return EffectiveFrom.Equals(other.EffectiveFrom) && Nullable.Equals(EffectiveTo, other.EffectiveTo);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EffectiveFrom, EffectiveTo);
    }
}
