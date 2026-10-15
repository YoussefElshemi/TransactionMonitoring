using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct SunsetOn : IFormattable
{
    public SunsetOn(DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(SunsetOn));
        Value = value;
    }

    private DateOnly Value { get; }

    public static implicit operator DateOnly(SunsetOn valueObject)
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
