using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ObservedAt : IFormattable
{
    public ObservedAt(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ObservedAt));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(ObservedAt valueObject)
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
