using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct RemainingPermits : IFormattable
{
    public RemainingPermits(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(RemainingPermits));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(RemainingPermits valueObject)
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
