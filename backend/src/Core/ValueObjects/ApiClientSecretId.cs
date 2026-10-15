using System.Globalization;

namespace Core.ValueObjects;

public readonly record struct ApiClientSecretId : IFormattable
{
    public ApiClientSecretId(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ApiClientSecretId));
        Value = value;
    }

    private Guid Value { get; }

    public static implicit operator Guid(ApiClientSecretId valueObject)
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
