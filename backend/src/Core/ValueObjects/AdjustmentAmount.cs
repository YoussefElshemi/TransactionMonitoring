using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct AdjustmentAmount : IFormattable
{
    public AdjustmentAmount(decimal value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(AdjustmentAmount));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(AdjustmentAmount valueObject)
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
