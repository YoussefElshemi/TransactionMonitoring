using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct PageCount : IFormattable
{
    public PageCount(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(PageCount));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(PageCount valueObject)
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