namespace Core.ValueObjects;

public readonly record struct EscalationRuleDescription
{
    public const int MaxLength = 512;

    public EscalationRuleDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(EscalationRuleDescription));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, MaxLength, nameof(EscalationRuleDescription));
        Value = value;
    }

    private string Value { get; }

    public static implicit operator string(EscalationRuleDescription valueObject)
    {
        return valueObject.Value;
    }

    public override string ToString()
    {
        return Value;
    }
}
