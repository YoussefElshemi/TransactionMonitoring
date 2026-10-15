using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct RiskScore : IFormattable
{
    public RiskScore(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(RiskScore));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(RiskScore valueObject)
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
