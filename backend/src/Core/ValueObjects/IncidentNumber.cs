namespace Core.ValueObjects;

public readonly record struct IncidentNumber
{
    public const int MaxLength = 32;

    public IncidentNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(IncidentNumber));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(IncidentNumber));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(IncidentNumber valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
