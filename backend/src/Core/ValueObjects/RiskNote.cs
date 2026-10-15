namespace Core.ValueObjects;

public readonly record struct RiskNote
{
    public const int MaxLength = 2000;

    public RiskNote(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(RiskNote));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(RiskNote));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(RiskNote valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
