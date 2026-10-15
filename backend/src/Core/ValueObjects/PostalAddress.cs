namespace Core.ValueObjects;

public readonly record struct PostalAddress
{
    public const int MaxLength = 500;

    public PostalAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(PostalAddress));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(PostalAddress));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PostalAddress valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
