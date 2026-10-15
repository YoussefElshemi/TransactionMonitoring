namespace Core.ValueObjects;

public readonly record struct IncidentUpdateBody
{
    public const int MaxLength = 4000;

    public IncidentUpdateBody(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(IncidentUpdateBody));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(IncidentUpdateBody));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(IncidentUpdateBody valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
