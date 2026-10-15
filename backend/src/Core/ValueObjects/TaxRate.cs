using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct TaxRate : IFormattable
{
    public TaxRate(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(TaxRate));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(TaxRate));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(TaxRate valueObject)
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
