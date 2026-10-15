namespace Core.ValueObjects;

public readonly record struct PartyAttributeName
{
    public const int MaxLength = 64;

    public PartyAttributeName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(PartyAttributeName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(PartyAttributeName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PartyAttributeName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
