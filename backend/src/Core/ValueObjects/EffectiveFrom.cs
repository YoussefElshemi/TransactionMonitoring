using System.Globalization;
using Core.Constants;

namespace Core.ValueObjects;

public readonly record struct EffectiveFrom : IFormattable
{
    public EffectiveFrom(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(EffectiveFrom));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(EffectiveFrom valueObject)
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
