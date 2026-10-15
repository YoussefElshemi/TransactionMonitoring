using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct QueuePriority : IFormattable
{
    public const int MaxValue = 1000;

    public QueuePriority(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(QueuePriority));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxValue, nameof(QueuePriority));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(QueuePriority valueObject)
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
