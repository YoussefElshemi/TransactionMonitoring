using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct PlatformFee : IFormattable
{
    public PlatformFee(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(PlatformFee));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(PlatformFee valueObject)
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
