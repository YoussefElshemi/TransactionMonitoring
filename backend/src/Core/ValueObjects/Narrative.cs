namespace Core.ValueObjects;

public readonly record struct Narrative
{
    public const int MaxLength = 32768;

    public Narrative(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Narrative));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(Narrative));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(Narrative valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
