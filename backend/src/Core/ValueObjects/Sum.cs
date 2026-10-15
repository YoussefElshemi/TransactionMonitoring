using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct Sum : IFormattable
{
    public Sum(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(Sum));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(Sum valueObject)
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