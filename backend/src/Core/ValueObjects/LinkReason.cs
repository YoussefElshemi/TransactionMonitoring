namespace Core.ValueObjects;

public readonly record struct LinkReason
{
    public LinkReason(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(LinkReason));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(LinkReason valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
