using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ConfirmedAt : IFormattable
{
    public ConfirmedAt(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ConfirmedAt));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(ConfirmedAt valueObject)
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
