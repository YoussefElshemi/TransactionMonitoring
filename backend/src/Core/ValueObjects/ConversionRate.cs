using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ConversionRate : IFormattable
{
    public ConversionRate(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0, nameof(ConversionRate));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(ConversionRate valueObject)
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
