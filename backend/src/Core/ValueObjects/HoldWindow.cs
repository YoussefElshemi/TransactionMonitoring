using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct HoldWindow : IFormattable
{
    public HoldWindow(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minutes, 1, nameof(HoldWindow));
        Value = minutes;
    }

    private int Value { get; }

    public TimeSpan ToTimeSpan()
    {
        return TimeSpan.FromMinutes(Value);
    }

    public static implicit operator int(HoldWindow valueObject)
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
