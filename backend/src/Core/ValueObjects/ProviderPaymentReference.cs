namespace Core.ValueObjects;

public readonly record struct ProviderPaymentReference
{
    public ProviderPaymentReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ProviderPaymentReference));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ProviderPaymentReference valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
