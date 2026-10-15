using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct TokensPerPeriod : IFormattable
{
    public TokensPerPeriod(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(TokensPerPeriod));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(TokensPerPeriod valueObject)
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
