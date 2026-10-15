namespace Core.ValueObjects;

public readonly record struct ThreatName
{
    public ThreatName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(ThreatName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(ThreatName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
