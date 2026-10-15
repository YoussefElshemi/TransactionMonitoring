using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct Sequence : IFormattable
{
    public Sequence(long value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(Sequence));
        Value = value;
    }

    private long Value { get; }

    public static implicit operator long(Sequence valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString(null, CultureInfo.InvariantCulture);
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}
