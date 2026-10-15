namespace Core.ValueObjects;

public readonly record struct PlanName
{
    public const int MaxLength = 200;

    public PlanName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(PlanName));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(PlanName));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(PlanName valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
