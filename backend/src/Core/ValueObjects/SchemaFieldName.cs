namespace Core.ValueObjects;

public readonly record struct SchemaFieldName
{
    public SchemaFieldName(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(SchemaFieldName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(SchemaFieldName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
