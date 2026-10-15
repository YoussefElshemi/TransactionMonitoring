namespace Core.ValueObjects;

public readonly record struct RemittanceReference
{
    public RemittanceReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(RemittanceReference));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(RemittanceReference valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
