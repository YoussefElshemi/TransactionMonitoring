using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct AlertId : IFormattable
{
    public AlertId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(AlertId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(AlertId valueObject)
    {
        return valueObject.Value;
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
