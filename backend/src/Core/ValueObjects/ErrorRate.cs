using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ErrorRate : IFormattable
{
    public ErrorRate(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(ErrorRate));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(ErrorRate));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(ErrorRate valueObject)
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
