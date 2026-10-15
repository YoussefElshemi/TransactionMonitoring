namespace Core.ValueObjects;

public readonly record struct RevealReason
{
    public const int MinLength = 3;
    public const int MaxLength = 512;

    public RevealReason(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(RevealReason));
        ArgumentOutOfRangeException.ThrowIfLessThan(value.Length, MinLength, nameof(RevealReason));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(RevealReason));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(RevealReason valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
