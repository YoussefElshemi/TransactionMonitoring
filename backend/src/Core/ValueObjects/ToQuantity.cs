using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ToQuantity : IFormattable
{
    public ToQuantity(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(ToQuantity));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(ToQuantity valueObject)
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
