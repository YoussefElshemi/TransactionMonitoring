using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ConcentrationIndex : IFormattable
{
    public ConcentrationIndex(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(ConcentrationIndex));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1, nameof(ConcentrationIndex));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(ConcentrationIndex valueObject)
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
