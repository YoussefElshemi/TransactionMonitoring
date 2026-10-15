using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct UnitCost : IFormattable
{
    public UnitCost(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(UnitCost));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(UnitCost valueObject)
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
