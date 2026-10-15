using System.Globalization;
using Core.Constants;

namespace Core.ValueObjects;

public readonly record struct ObservationDate : IComparable<ObservationDate>, IFormattable
{
    public ObservationDate(DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ObservationDate));
        Value = value;
    }

    private DateOnly Value { get; }

    public static implicit operator DateOnly(ObservationDate valueObject)
    {
        return valueObject.Value;
    }

    public int CompareTo(ObservationDate other)
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
