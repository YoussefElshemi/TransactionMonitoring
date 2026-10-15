using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct VersionNumber : IFormattable
{
    public VersionNumber(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(VersionNumber));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(VersionNumber valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value.ToString(null, null);
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}
