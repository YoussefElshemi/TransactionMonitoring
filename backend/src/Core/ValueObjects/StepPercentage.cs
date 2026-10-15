using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct StepPercentage : IFormattable
{
    public StepPercentage(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(StepPercentage));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(StepPercentage));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(StepPercentage valueObject)
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
