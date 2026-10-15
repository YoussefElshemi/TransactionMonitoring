namespace Core.ValueObjects;

public readonly record struct UserName
{
    public UserName(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(UserName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(UserName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}