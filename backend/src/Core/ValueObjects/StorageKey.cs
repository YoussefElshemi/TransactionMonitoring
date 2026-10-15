namespace Core.ValueObjects;

public readonly record struct StorageKey
{
    public StorageKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(StorageKey));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(StorageKey valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
