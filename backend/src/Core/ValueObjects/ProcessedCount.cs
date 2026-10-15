using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ProcessedCount : IFormattable
{
    public ProcessedCount(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ProcessedCount));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(ProcessedCount valueObject)
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