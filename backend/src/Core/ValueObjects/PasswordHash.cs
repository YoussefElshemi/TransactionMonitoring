namespace Core.ValueObjects;

public readonly record struct PasswordHash
{
    public PasswordHash(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(PasswordHash));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PasswordHash valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}