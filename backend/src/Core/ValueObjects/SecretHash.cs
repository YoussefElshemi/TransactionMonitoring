namespace Core.ValueObjects;

public readonly record struct SecretHash
{
    public SecretHash(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(SecretHash));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(SecretHash valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
