namespace Core.ValueObjects;

public readonly record struct IncidentSummary
{
    public const int MaxLength = 2000;

    public IncidentSummary(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(IncidentSummary));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(IncidentSummary));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(IncidentSummary valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
