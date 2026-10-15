using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ReleasedOn : IFormattable
{
    public ReleasedOn(DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ReleasedOn));
        Value = value;
    }

    private DateOnly Value { get; }

    public static implicit operator DateOnly(ReleasedOn valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString("O", CultureInfo.InvariantCulture);
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}
