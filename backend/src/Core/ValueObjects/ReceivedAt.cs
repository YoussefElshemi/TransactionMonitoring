using System.Globalization;
using Core.Constants;

namespace Core.ValueObjects;

public readonly record struct ReceivedAt : IFormattable
{
    public ReceivedAt(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ReceivedAt));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(ReceivedAt valueObject)
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
