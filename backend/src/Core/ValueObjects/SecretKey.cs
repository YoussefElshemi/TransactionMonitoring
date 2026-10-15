namespace Core.ValueObjects;

public readonly record struct SecretKey
{
    public SecretKey(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(SecretKey));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(SecretKey valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}