using Core.Constants;

namespace Core.ValueObjects;

public readonly record struct WebhookEvent
{
    public WebhookEvent(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(WebhookEvent));

        if (!WebhookEvents.All.Contains(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Not a published webhook event.");
        }

        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(WebhookEvent valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
