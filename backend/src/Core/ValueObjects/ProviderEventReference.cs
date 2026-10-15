namespace Core.ValueObjects;

public readonly record struct ProviderEventReference
{
    public ProviderEventReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ProviderEventReference));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ProviderEventReference valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
