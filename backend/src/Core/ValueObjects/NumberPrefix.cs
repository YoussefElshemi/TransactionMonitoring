namespace Core.ValueObjects;

public readonly record struct NumberPrefix
{
    public const int MaxLength = 8;

    public NumberPrefix(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(NumberPrefix));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(NumberPrefix));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(NumberPrefix valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
