namespace Core.ValueObjects;

public readonly record struct SecretPrefix
{
    public SecretPrefix(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(SecretPrefix));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(SecretPrefix valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
