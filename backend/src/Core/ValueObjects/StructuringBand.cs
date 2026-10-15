using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct StructuringBand : IFormattable
{
    public StructuringBand(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0m, nameof(StructuringBand));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1m, nameof(StructuringBand));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(StructuringBand valueObject)
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
