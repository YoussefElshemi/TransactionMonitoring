using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct BalanceAmount : IFormattable
{
    public BalanceAmount(decimal value)
    {
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(BalanceAmount valueObject)
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
