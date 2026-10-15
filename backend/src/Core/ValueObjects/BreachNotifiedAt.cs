using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct BreachNotifiedAt : IFormattable
{
    public BreachNotifiedAt(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(BreachNotifiedAt));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(BreachNotifiedAt valueObject)
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
