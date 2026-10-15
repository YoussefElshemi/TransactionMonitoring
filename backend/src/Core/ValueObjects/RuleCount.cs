using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct RuleCount : IFormattable
{
    public RuleCount(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(RuleCount));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(RuleCount valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}
