using System.Globalization;
using Core.Constants;

namespace Core.ValueObjects;

public readonly record struct HeldAt : IFormattable
{
    public HeldAt(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(HeldAt));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(HeldAt valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString(DateTimeConstants.DateTimeFormat);
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        format ??= DateTimeConstants.DateTimeFormat;
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}
