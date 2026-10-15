namespace Core.ValueObjects;

public readonly record struct ProviderEventType
{
    public ProviderEventType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ProviderEventType));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ProviderEventType valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
