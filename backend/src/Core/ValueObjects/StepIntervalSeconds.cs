using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct StepIntervalSeconds : IFormattable
{
    public StepIntervalSeconds(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(StepIntervalSeconds));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(StepIntervalSeconds valueObject)
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
