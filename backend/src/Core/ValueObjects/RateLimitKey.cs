namespace Core.ValueObjects;

public readonly record struct RateLimitKey
{
    public RateLimitKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(RateLimitKey));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(RateLimitKey valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
