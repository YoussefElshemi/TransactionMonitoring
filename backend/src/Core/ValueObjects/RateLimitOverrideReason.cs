namespace Core.ValueObjects;

public readonly record struct RateLimitOverrideReason
{
    public const int MaxLength = 500;

    public RateLimitOverrideReason(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(RateLimitOverrideReason));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(RateLimitOverrideReason));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(RateLimitOverrideReason valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
