using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct RetryAfter : IFormattable
{
    public RetryAfter(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(RetryAfter));
        Value = value;
    }

    private int Value { get; }

    public TimeSpan ToTimeSpan()
    {
        return TimeSpan.FromSeconds(Value);
    }

    public static implicit operator int(RetryAfter valueObject)
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
