using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct Year : IFormattable
{
    public Year(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(Year));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(Year valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}