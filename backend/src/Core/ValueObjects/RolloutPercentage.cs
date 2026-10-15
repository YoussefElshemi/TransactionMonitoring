using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct RolloutPercentage : IFormattable
{
    public RolloutPercentage(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(RolloutPercentage));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(RolloutPercentage));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(RolloutPercentage valueObject)
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
