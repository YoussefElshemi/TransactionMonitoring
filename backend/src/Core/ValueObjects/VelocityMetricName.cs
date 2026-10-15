namespace Core.ValueObjects;

public readonly record struct VelocityMetricName
{
    public const int MaxLength = 64;

    public VelocityMetricName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(VelocityMetricName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(VelocityMetricName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(VelocityMetricName valueObject)
    {
        return valueObject.Value;
    }

    // Case-insensitive: the engine's window key uses the rule's spelling while the stored key uses the declared one, and a miss silently reads as an empty window.
    public bool Equals(VelocityMetricName other)
    {
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }
}
