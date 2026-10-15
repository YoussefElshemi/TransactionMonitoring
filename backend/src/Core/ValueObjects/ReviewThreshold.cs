using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ReviewThreshold : IFormattable
{
    public ReviewThreshold(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(ReviewThreshold));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(ReviewThreshold valueObject)
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
