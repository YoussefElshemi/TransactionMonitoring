namespace Core.ValueObjects;

public readonly record struct FailureCode
{
    public FailureCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(FailureCode));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(FailureCode valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
