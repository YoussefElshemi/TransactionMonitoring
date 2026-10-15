using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct CaseGroupingPeriod : IFormattable
{
    public CaseGroupingPeriod(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minutes, 1, nameof(CaseGroupingPeriod));
        Value = minutes;
    }

    private int Value { get; }

    public TimeSpan ToTimeSpan()
    {
        return TimeSpan.FromMinutes(Value);
    }

    public static implicit operator int(CaseGroupingPeriod valueObject)
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
