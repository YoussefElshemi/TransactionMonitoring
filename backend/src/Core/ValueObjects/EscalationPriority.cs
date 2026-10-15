using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct EscalationPriority : IFormattable
{
    public const int MaxValue = 1000;

    public EscalationPriority(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(EscalationPriority));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxValue, nameof(EscalationPriority));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(EscalationPriority valueObject)
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
