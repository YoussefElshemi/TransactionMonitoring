namespace Core.ValueObjects;

public readonly record struct InvoiceAdjustmentDescription
{
    public const int MaxLength = 500;

    public InvoiceAdjustmentDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(InvoiceAdjustmentDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(InvoiceAdjustmentDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(InvoiceAdjustmentDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
