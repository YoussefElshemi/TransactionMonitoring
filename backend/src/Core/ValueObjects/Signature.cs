namespace Core.ValueObjects;

public readonly record struct Signature
{
    public Signature(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(Signature));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Signature valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}