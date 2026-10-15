using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct MaxAlertsPerCase : IFormattable
{
    public MaxAlertsPerCase(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(MaxAlertsPerCase));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(MaxAlertsPerCase valueObject)
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
