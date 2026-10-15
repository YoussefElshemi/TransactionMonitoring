using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ConsecutiveFailures : IFormattable
{
    public ConsecutiveFailures(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(ConsecutiveFailures));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(ConsecutiveFailures valueObject)
    {
        return valueObject.Value;
    }

    public static ConsecutiveFailures operator +(ConsecutiveFailures a, int b)
    {
        return new ConsecutiveFailures(a.Value + b);
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
