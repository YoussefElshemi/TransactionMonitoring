namespace Core.ValueObjects;

public readonly record struct WrappedDataKey
{
    public const int MaxLength = 256;

    public WrappedDataKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(WrappedDataKey));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(WrappedDataKey));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(WrappedDataKey valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
