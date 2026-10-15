using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct FiledAt : IFormattable
{
    public FiledAt(DateTimeOffset value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(FiledAt));
        Value = value;
    }

    private DateTimeOffset Value { get; }

    public static implicit operator DateTimeOffset(FiledAt valueObject)
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
