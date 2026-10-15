namespace Core.ValueObjects;

public readonly record struct ReceiptNumber
{
    public ReceiptNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ReceiptNumber));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ReceiptNumber valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
