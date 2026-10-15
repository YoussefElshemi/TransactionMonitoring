namespace Core.ValueObjects;

public readonly record struct PartyDisplayName
{
    public const int MaxLength = 512;

    public PartyDisplayName(string value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(PartyDisplayName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(PartyDisplayName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PartyDisplayName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
