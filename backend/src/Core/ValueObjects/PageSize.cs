using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct PageSize : IFormattable
{
    public PageSize(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(PageSize));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(PageSize valueObject)
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