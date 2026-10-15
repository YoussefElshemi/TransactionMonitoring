using System.Globalization;
using Core.Constants;

namespace Core.ValueObjects;

public readonly record struct CreatedAt : IFormattable
{
    public CreatedAt(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(CreatedAt));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(CreatedAt valueObject)
    {
        return valueObject.Value;
    }

    public DateTimeOffset AddDays(double value) => Value.AddDays(value);

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