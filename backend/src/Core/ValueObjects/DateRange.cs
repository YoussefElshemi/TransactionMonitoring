using System.Globalization;
using Core.Constants;

namespace Core.ValueObjects;

public readonly record struct DateRange : IComparable<DateOnly>, IComparable<DateRange>, IFormattable
{
    public DateRange(DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    private DateOnly Value { get; }

    public static implicit operator DateOnly(DateRange valueObject)
    {
        return valueObject.Value;
    }

    public int CompareTo(DateOnly other)
    {
        return Value.CompareTo(other);
    }

    public int CompareTo(DateRange other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return Value.ToString(DateTimeConstants.DateFormat);
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}