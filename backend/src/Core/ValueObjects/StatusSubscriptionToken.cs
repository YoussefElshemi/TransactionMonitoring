namespace Core.ValueObjects;

public readonly record struct StatusSubscriptionToken
{
    public const int MaxLength = 128;

    public StatusSubscriptionToken(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(StatusSubscriptionToken));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(StatusSubscriptionToken));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(StatusSubscriptionToken valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
