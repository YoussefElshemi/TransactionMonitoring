namespace Core.ValueObjects;

public readonly record struct Message
{
    public Message(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(Message));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Message valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}