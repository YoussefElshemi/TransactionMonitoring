using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct MinimumCharge : IFormattable
{
    public MinimumCharge(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(MinimumCharge));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(MinimumCharge valueObject)
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
