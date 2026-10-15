using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct StatusCode : IFormattable
{
    public StatusCode(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(StatusCode));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(StatusCode valueObject)
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
