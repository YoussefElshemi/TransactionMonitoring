namespace Core.ValueObjects;

public readonly record struct PaymentMethodReference
{
    public PaymentMethodReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(PaymentMethodReference));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PaymentMethodReference valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
