using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ByteCount : IFormattable
{
    public ByteCount(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(ByteCount));
        Value = value;
    }

    private long Value { get; }

    public static implicit operator long(ByteCount valueObject)
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
