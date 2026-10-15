using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct SlaTarget : IFormattable
{
    public const int MaxValue = 525600;

    public SlaTarget(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minutes, 1, nameof(SlaTarget));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minutes, MaxValue, nameof(SlaTarget));
        Value = minutes;
    }

    private int Value { get; }

    public TimeSpan ToTimeSpan()
    {
        return TimeSpan.FromMinutes(Value);
    }

    public static implicit operator int(SlaTarget valueObject)
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
