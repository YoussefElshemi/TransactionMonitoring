using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ScheduledStart : IFormattable
{
    public ScheduledStart(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ScheduledStart));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(ScheduledStart valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}
