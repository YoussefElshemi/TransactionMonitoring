using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct LatencyTarget : IFormattable
{
    public const int MaxValue = 600000;

    public LatencyTarget(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(LatencyTarget));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxValue, nameof(LatencyTarget));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(LatencyTarget valueObject)
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
