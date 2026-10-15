namespace Core.ValueObjects;

public readonly record struct PartyIdentifier
{
    public const int MaxLength = 256;

    public PartyIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(PartyIdentifier));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(PartyIdentifier));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PartyIdentifier valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
