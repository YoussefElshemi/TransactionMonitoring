using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct Count : IFormattable
{
    public Count(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(Count));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(Count valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString(null, null);
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}