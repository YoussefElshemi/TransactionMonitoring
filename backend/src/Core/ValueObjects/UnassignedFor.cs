using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct UnassignedFor : IFormattable
{
    public const int MaxValue = 525600;

    public UnassignedFor(int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minutes, 1, nameof(UnassignedFor));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minutes, MaxValue, nameof(UnassignedFor));
        Value = minutes;
    }

    private int Value { get; }

    public TimeSpan ToTimeSpan()
    {
        return TimeSpan.FromMinutes(Value);
    }

    public static implicit operator int(UnassignedFor valueObject)
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
