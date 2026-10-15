namespace Core.ValueObjects;

public readonly record struct FailureReason
{
    public FailureReason(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(FailureReason));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(FailureReason valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
