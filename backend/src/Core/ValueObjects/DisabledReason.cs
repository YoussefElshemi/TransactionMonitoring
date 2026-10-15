namespace Core.ValueObjects;

public readonly record struct DisabledReason
{
    public DisabledReason(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(DisabledReason));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(DisabledReason valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
