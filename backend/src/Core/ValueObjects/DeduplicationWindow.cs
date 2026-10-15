using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct DeduplicationWindow : IFormattable
{
    public DeduplicationWindow(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minutes, 1, nameof(DeduplicationWindow));
        Value = minutes;
    }

    private int Value { get; }

    public TimeSpan ToTimeSpan()
    {
        return TimeSpan.FromMinutes(Value);
    }

    public static implicit operator int(DeduplicationWindow valueObject)
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
