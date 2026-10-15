namespace Core.ValueObjects;

public readonly record struct ClientId
{
    public ClientId(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ClientId));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ClientId valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
