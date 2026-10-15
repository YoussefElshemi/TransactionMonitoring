namespace Core.ValueObjects;

public readonly record struct MandateReference
{
    public MandateReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(MandateReference));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(MandateReference valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
