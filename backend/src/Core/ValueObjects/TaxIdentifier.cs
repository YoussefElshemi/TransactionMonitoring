namespace Core.ValueObjects;

public readonly record struct TaxIdentifier
{
    public const int MaxLength = 32;

    public TaxIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(TaxIdentifier));

        var normalised = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        ArgumentException.ThrowIfNullOrWhiteSpace(normalised, nameof(TaxIdentifier));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(normalised.Length, MaxLength, nameof(TaxIdentifier));

        Value = normalised;
    }

    private string Value { get; }

    public static implicit operator string(TaxIdentifier valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
