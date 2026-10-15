using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct Attempts : IFormattable
{
    public Attempts(int value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(Attempts));
        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(Attempts valueObject)
    {
        return valueObject.Value;
    }

    public static Attempts operator +(Attempts a, int b)
    {
        return new Attempts(a.Value + b);
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.InvariantCulture;
        return Value.ToString(format, formatProvider);
    }
}