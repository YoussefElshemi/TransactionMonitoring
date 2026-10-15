using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct EntryAmount : IFormattable
{
    public EntryAmount(decimal value)
    {
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(EntryAmount), value, "An account entry must move an amount.");
        }

        Value = value;
    }

    private decimal Value { get; }

    public static implicit operator decimal(EntryAmount valueObject)
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
