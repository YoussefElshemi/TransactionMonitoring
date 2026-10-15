using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ServiceCreditAmount : IFormattable
{
    public ServiceCreditAmount(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(ServiceCreditAmount));
        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(ServiceCreditAmount valueObject)
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
