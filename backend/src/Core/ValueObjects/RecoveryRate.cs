using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct RecoveryRate : IFormattable
{
    public RecoveryRate(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(RecoveryRate));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(RecoveryRate));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(RecoveryRate valueObject)
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
