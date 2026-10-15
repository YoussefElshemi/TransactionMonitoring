using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ChurnRate : IFormattable
{
    public ChurnRate(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(ChurnRate));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(ChurnRate));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(ChurnRate valueObject)
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
