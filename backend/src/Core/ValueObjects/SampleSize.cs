using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct SampleSize : IFormattable
{
    public SampleSize(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(SampleSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(SampleSize));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(SampleSize valueObject)
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
