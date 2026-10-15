namespace Core.ValueObjects;

public readonly record struct Resource
{
    public Resource(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(Resource));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Resource valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}