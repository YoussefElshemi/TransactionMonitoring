namespace Core.ValueObjects;

public readonly record struct MetricText
{
    public const int MaxLength = 256;

    public MetricText(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(MetricText));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(MetricText));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(MetricText valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
