namespace Core.ValueObjects;

public readonly record struct Payload
{
    public Payload(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(Payload));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Payload valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}