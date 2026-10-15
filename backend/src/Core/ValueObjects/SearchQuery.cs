namespace Core.ValueObjects;

public readonly record struct SearchQuery
{
    // Three is the shortest fragment a trigram index can serve.
    public const int MinimumLength = 3;
    public const int MaximumLength = 100;

    public SearchQuery(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(SearchQuery));

        var trimmed = value.Trim();

        ArgumentOutOfRangeException.ThrowIfLessThan(trimmed.Length, MinimumLength, nameof(SearchQuery));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trimmed.Length, MaximumLength, nameof(SearchQuery));

        Value = trimmed;
    }

    private string Value { get; }

    public static implicit operator string(SearchQuery valueObject)
    {
        return valueObject.Value;
    }

    public static bool IsValid(string? value)
    {
        return value?.Trim().Length is >= MinimumLength and <= MaximumLength;
    }

    public override string ToString()
    {
        return Value;
    }
}
