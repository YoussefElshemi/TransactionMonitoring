namespace Core.ValueObjects;

public readonly record struct CountryCode
{
    public const int Length = 2;

    public CountryCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(CountryCode));

        var normalised = value.Trim().ToUpperInvariant();

        if (normalised.Length != Length || !normalised.All(char.IsAsciiLetterUpper))
        {
            throw new ArgumentOutOfRangeException(nameof(CountryCode), value, "A country code is two letters.");
        }

        Value = normalised;
    }

    private string Value { get; }

    public static implicit operator string(CountryCode valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
