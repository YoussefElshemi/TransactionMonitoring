namespace Core.ValueObjects;

public readonly record struct ProviderIdempotencyKey
{
    public ProviderIdempotencyKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ProviderIdempotencyKey));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ProviderIdempotencyKey valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
