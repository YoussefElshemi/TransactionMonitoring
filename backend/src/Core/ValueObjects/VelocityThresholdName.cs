namespace Core.ValueObjects;

public readonly record struct VelocityThresholdName
{
    public const int MaxLength = 64;

    public VelocityThresholdName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(VelocityThresholdName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(VelocityThresholdName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(VelocityThresholdName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
