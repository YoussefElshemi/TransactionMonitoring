using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct FreeAllowance : IFormattable
{
    public FreeAllowance(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(FreeAllowance));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(FreeAllowance valueObject)
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
