namespace Core.ValueObjects;

public readonly record struct ContentType
{
    public ContentType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ContentType));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ContentType valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
