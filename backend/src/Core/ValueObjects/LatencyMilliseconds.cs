using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct LatencyMilliseconds : IFormattable
{
    public LatencyMilliseconds(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(LatencyMilliseconds));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(LatencyMilliseconds valueObject)
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
