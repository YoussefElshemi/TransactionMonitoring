using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct UptimePercentage : IFormattable
{
    public UptimePercentage(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(UptimePercentage));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(UptimePercentage));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(UptimePercentage valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}
