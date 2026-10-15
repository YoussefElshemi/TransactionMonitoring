namespace Core.ValueObjects;

public readonly record struct DocumentNumber
{
    public DocumentNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(DocumentNumber));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(DocumentNumber valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
