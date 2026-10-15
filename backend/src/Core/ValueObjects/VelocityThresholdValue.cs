using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct VelocityThresholdValue : IFormattable
{
    public VelocityThresholdValue(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0m, nameof(VelocityThresholdValue));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(VelocityThresholdValue valueObject)
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
