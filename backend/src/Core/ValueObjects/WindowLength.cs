using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct WindowLength : IFormattable
{
    public WindowLength(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(WindowLength));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(WindowLength valueObject)
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
