using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct LatencyPercentile : IFormattable
{
    public LatencyPercentile(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(LatencyPercentile));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(LatencyPercentile));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(LatencyPercentile valueObject)
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
