using System.Globalization;
using Core.Constants;

namespace Core.ValueObjects;

public readonly record struct WebhookPayloadVersion : IFormattable
{
    public WebhookPayloadVersion(int value)
    {
        if (!WebhookPayloadVersions.Supported.Contains(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Not a supported webhook payload version.");
        }

        Value = value;
    }

    private int Value { get; }

    public static implicit operator int(WebhookPayloadVersion valueObject)
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
