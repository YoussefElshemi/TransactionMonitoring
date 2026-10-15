using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ScorePenalty : IFormattable
{
    public ScorePenalty(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(ScorePenalty));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100, nameof(ScorePenalty));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(ScorePenalty valueObject)
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
