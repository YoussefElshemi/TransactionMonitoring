namespace Core.ValueObjects;

public readonly record struct ProviderCustomerReference
{
    public ProviderCustomerReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ProviderCustomerReference));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ProviderCustomerReference valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
