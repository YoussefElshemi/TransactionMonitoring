using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ReplenishmentPeriod : IFormattable
{
    public ReplenishmentPeriod(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(ReplenishmentPeriod));
        Value = value;
    }

    private int Value { get; }

    public TimeSpan ToTimeSpan()
    {
        return TimeSpan.FromSeconds(Value);
    }

    public static implicit operator int(ReplenishmentPeriod valueObject)
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
