using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct PaymentTerms : IFormattable
{
    public const int MaxDays = 365;

    public PaymentTerms(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(PaymentTerms));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxDays, nameof(PaymentTerms));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(PaymentTerms valueObject)
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
