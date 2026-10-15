namespace Core.ValueObjects;

public readonly record struct InvoiceLineDescription
{
    public const int MaxLength = 500;

    public InvoiceLineDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(InvoiceLineDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(InvoiceLineDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(InvoiceLineDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
