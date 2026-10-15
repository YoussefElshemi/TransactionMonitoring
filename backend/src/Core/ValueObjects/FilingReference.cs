namespace Core.ValueObjects;

public readonly record struct FilingReference
{
    public const int MaxLength = 256;

    public FilingReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(FilingReference));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(FilingReference));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(FilingReference valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
