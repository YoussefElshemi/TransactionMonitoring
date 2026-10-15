using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct TotalLatencyMilliseconds : IFormattable
{
    public TotalLatencyMilliseconds(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(TotalLatencyMilliseconds));
        Value = value;
    }

    private long Value { get; }

    public static implicit operator long(TotalLatencyMilliseconds valueObject)
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
