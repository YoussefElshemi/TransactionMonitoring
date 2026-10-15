namespace Core.ValueObjects;

public readonly record struct Email
{
    public Email(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(Email));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Email valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}