namespace Core.ValueObjects;

public readonly record struct PlanDescription
{
    public const int MaxLength = 1000;

    public PlanDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(PlanDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(PlanDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PlanDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
