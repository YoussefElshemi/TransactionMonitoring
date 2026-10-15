using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct CostPerRequest : IFormattable
{
    public CostPerRequest(decimal value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(CostPerRequest valueObject)
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