namespace Core.ValueObjects;

public readonly record struct TaxIdentifierPattern
{
    public const int MaxLength = 200;

    public TaxIdentifierPattern(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(TaxIdentifierPattern));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(TaxIdentifierPattern));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(TaxIdentifierPattern valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
