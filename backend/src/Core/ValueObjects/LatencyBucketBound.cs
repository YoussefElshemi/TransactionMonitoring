using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct LatencyBucketBound : IFormattable
{
    public LatencyBucketBound(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(LatencyBucketBound));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(LatencyBucketBound valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString(null, null);
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}
