using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct FilingDeadline : IFormattable
{
    public const int MaxValue = 365;

    public FilingDeadline(int days)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(days, 1, nameof(FilingDeadline));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(days, MaxValue, nameof(FilingDeadline));
        Value = days;
    }

    private int Value { get; }

    public TimeSpan ToTimeSpan()
    {
        return TimeSpan.FromDays(Value);
    }

    public static implicit operator int(FilingDeadline valueObject)
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
