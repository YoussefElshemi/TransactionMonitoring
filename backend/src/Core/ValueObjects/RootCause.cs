namespace Core.ValueObjects;

public readonly record struct RootCause
{
    public const int MaxLength = 4000;

    public RootCause(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(RootCause));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(RootCause));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(RootCause valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
