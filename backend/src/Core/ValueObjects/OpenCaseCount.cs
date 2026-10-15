using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct OpenCaseCount : IFormattable
{
    public OpenCaseCount(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(OpenCaseCount));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(OpenCaseCount valueObject)
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
