namespace Core.ValueObjects;

public readonly record struct ClientSecret
{
    public ClientSecret(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(ClientSecret));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ClientSecret valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
