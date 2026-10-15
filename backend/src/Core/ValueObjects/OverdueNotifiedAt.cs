using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct OverdueNotifiedAt : IFormattable
{
    public OverdueNotifiedAt(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(OverdueNotifiedAt));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(OverdueNotifiedAt valueObject)
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
