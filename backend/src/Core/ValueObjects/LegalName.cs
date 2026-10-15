namespace Core.ValueObjects;

public readonly record struct LegalName
{
    public const int MaxLength = 200;

    public LegalName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(LegalName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(LegalName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(LegalName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
