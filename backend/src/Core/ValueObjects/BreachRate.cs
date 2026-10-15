using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct BreachRate : IFormattable
{
    public BreachRate(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(BreachRate));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(BreachRate));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(BreachRate valueObject)
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
