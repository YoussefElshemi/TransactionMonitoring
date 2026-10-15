using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct AvailabilityTarget : IFormattable
{
    public AvailabilityTarget(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(AvailabilityTarget));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(AvailabilityTarget));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(AvailabilityTarget valueObject)
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
