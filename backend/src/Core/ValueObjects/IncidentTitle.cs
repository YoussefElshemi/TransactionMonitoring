namespace Core.ValueObjects;

public readonly record struct IncidentTitle
{
    public const int MaxLength = 200;

    public IncidentTitle(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(IncidentTitle));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(IncidentTitle));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(IncidentTitle valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
