using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct Rank : IFormattable
{
    public Rank(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(Rank));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(Rank valueObject)
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
