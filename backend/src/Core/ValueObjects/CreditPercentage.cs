using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct CreditPercentage : IFormattable
{
    public CreditPercentage(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(CreditPercentage));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(CreditPercentage));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(CreditPercentage valueObject)
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
