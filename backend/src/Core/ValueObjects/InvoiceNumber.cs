namespace Core.ValueObjects;

public readonly record struct InvoiceNumber
{
    public const int MaxLength = 32;

    public InvoiceNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(InvoiceNumber));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(InvoiceNumber));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(InvoiceNumber valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
