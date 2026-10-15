using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct HealthScore : IFormattable
{
    public HealthScore(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(HealthScore));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(HealthScore));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(HealthScore valueObject)
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
