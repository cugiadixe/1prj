using System;
using System.Linq;

namespace PTKD.Domain.ValueObjects;

public sealed class RowVersion
{
    public byte[] Value { get; }

    private RowVersion(byte[] value)
    {
        if (value == null || value.Length != 8)
            throw new ArgumentException("RowVersion must be exactly 8 bytes.");
        Value = value;
    }

    public static RowVersion FromByteArray(byte[] value)
    {
        return new RowVersion(value);
    }

    public static RowVersion FromBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new ArgumentException("RowVersion base64 string cannot be empty or whitespace.");

        try
        {
            var bytes = Convert.FromBase64String(base64);
            return new RowVersion(bytes);
        }
        catch (FormatException)
        {
            throw new FormatException("Invalid base64 format for RowVersion.");
        }
    }

    public string ToBase64()
    {
        return Convert.ToBase64String(Value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        var other = (RowVersion)obj;
        return Value.SequenceEqual(other.Value);
    }

    public override int GetHashCode()
    {
        if (Value == null) return 0;
        int hash = 17;
        foreach (var b in Value)
        {
            hash = hash * 31 + b.GetHashCode();
        }
        return hash;
    }
}
