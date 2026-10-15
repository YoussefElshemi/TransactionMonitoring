namespace Core.ValueObjects;

public readonly record struct PaymentMethodLabel
{
    public PaymentMethodLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(PaymentMethodLabel));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PaymentMethodLabel valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
